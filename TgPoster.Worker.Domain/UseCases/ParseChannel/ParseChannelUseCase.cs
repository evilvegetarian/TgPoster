using MassTransit;
using Microsoft.Extensions.Logging;
using Shared.Enums;
using TgPoster.Telegram.Abstractions;
using TgPoster.Telegram.Models;
using TgPoster.Worker.Domain.Services.AdDetection;
using TgPoster.Worker.Domain.Services.ScheduleAlerts;

namespace TgPoster.Worker.Domain.UseCases.ParseChannel;

internal class ParseChannelUseCase(
	IParseChannelUseCaseStorage storage,
	IParseChannelRunStorage runStorage,
	IPublishEndpoint publishEndpoint,
	ITelegramMessageService tgMessages,
	IScheduleAlertService alertService,
	TimeProvider timeProvider,
	ILogger<ParseChannelUseCase> logger)
{
	/// <summary>
	/// Парсит канал-источник и отправляет новые посты на обработку. False — парсинг не удался насовсем:
	/// настроек нет или канал не найден либо недоступен (тогда источник уже помечен ошибкой). Временные
	/// сбои Telegram (FloodWait, таймаут) не считаются неудачей — источник попробуют в следующий раз
	/// </summary>
	/// <param name="id"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	public async Task<bool> Handle(Guid id, CancellationToken ct)
	{
		var parameters = await storage.GetChannelParsingParametersAsync(id, ct);
		if (parameters is null)
		{
			logger.LogError("Параметров нет, интересно почему..... Id: {id}", id);
			return false;
		}

		logger.LogInformation("Начали парсить данный канал: {ChannelName}", parameters.ChannelName);
		var startedAt = timeProvider.GetUtcNow();
		await storage.UpdateInHandleStatusAsync(id, ct);

		var channelName = parameters.ChannelName;
		var fromDate = parameters.FromDate;
		var toDate = parameters.ToDate;
		var avoidWords = parameters.AvoidWords;
		var lastParseId = parameters.LastParsedId;
		var checkNewPosts = parameters.CheckNewPosts;
		var telegramBotId = parameters.TelegramBotId;
		var sessionId = parameters.TelegramSessionId;

		var resolveResult = await tgMessages.ResolveChannelAsync(sessionId, channelName, ct);
		if (!resolveResult.IsSuccess)
		{
			logger.LogError("Не удалось найти канал по имени: {ChannelName} ({Status})", channelName,
				resolveResult.Status);
			if (IsSourceUnavailable(resolveResult.Status))
			{
				await MarkSourceUnavailableAsync(id, parameters, resolveResult.Status, ct);
				await AddFailedRunAsync(id, startedAt, ParseChannelRunStatus.SourceUnavailable,
					FormatError(resolveResult.Status, resolveResult.ErrorMessage), ct);
				return false;
			}

			await storage.ReleaseAfterTemporaryFailureAsync(id, ct);
			await AddFailedRunAsync(id, startedAt, ParseChannelRunStatus.TemporaryFailure,
				FormatError(resolveResult.Status, resolveResult.ErrorMessage), ct);
			return true;
		}

		var channel = resolveResult.Value!;
		List<TelegramMessage> allMessages = [];
		var newestId = lastParseId ?? 0;
		const int limit = 100;

		// История идёт от новых к старым: offset_id листает вниз, min_id отсекает уже спарсенное.
		// Сбой посреди истории не двигает позицию и ничего не публикует — в следующий раз заберём всё заново
		var offset = 0;
		while (true)
		{
			var historyResult = await tgMessages.GetHistoryAsync(
				sessionId,
				channel.Peer,
				limit,
				offsetDate: toDate ?? DateTime.Now,
				offsetId: offset,
				minId: lastParseId ?? 0,
				ct: ct);

			if (!historyResult.IsSuccess)
			{
				logger.LogError("Ошибка при получении истории канала {ChannelName}: {Status} {Error}",
					channelName, historyResult.Status, historyResult.ErrorMessage);
				if (IsSourceUnavailable(historyResult.Status))
				{
					await MarkSourceUnavailableAsync(id, parameters, historyResult.Status, ct);
					await AddFailedRunAsync(id, startedAt, ParseChannelRunStatus.SourceUnavailable,
						FormatError(historyResult.Status, historyResult.ErrorMessage), ct);
					return false;
				}

				await storage.ReleaseAfterTemporaryFailureAsync(id, ct);
				await AddFailedRunAsync(id, startedAt, ParseChannelRunStatus.TemporaryFailure,
					FormatError(historyResult.Status, historyResult.ErrorMessage), ct);
				return true;
			}

			var page = historyResult.Value!.Messages
				.Where(x => lastParseId is null || x.Id > lastParseId)
				.ToList();
			if (page.Count == 0)
			{
				break;
			}

			allMessages.AddRange(page.Where(x => fromDate is null || x.Date >= fromDate));
			newestId = Math.Max(newestId, page.Max(x => x.Id));

			var oldestId = page.Min(x => x.Id);
			var reachedEnd = historyResult.Value.Messages.Count < limit
			                 || page.Any(x => x.Date < fromDate)
			                 || (offset != 0 && oldestId >= offset);
			if (reachedEnd)
			{
				break;
			}

			offset = oldestId;
		}

		var groupedMessages = new Dictionary<long, List<TelegramMessage>>();
		long i = 1;
		foreach (var message in allMessages.OrderBy(m => m.Id))
		{
			if (message.GroupedId is { } groupedId)
			{
				if (!groupedMessages.ContainsKey(groupedId))
				{
					groupedMessages[groupedId] = [];
				}

				groupedMessages[groupedId].Add(message);
			}
			else
			{
				groupedMessages[i++] = [message];
			}
		}

		var validGroups = groupedMessages
			.Where(group => !group.Value.Any(msg => msg.Text != null
			                                        && avoidWords.Any(word =>
				                                        msg.Text.Contains(word, StringComparison.OrdinalIgnoreCase))
			)).ToList();
		var skippedByAvoidWords = groupedMessages.Count - validGroups.Count;

		var beforeAds = validGroups.Count;
		if (parameters.SkipAds)
		{
			validGroups = SkipAdGroups(validGroups, channelName);
		}

		var skippedAsAds = beforeAds - validGroups.Count;

		var beforeDuplicates = validGroups.Count;
		validGroups = await SkipAlreadyParsedAsync(id, validGroups, channelName, ct);
		var skippedAsDuplicates = beforeDuplicates - validGroups.Count;

		logger.LogDebug("Найдено {Count} новых постов для обработки.", validGroups.Count);

		var published = 0;
		var skippedEmpty = 0;
		foreach (var group in validGroups)
		{
			var command = new ProcessMessage
			{
				Id = id,
				TelegramBotId = telegramBotId,
				Messages = group.Value.Select(m => new MessageCommand
				{
					Id = m.Id,
					IsPhoto = m.Media?.Type == TelegramMediaType.Photo,
					IsVideo = m.Media?.Type is TelegramMediaType.Video or TelegramMediaType.Document
				}).ToList()
			};

			if (group.Value.All(p => p.Text is null && p.Media is null))
			{
				skippedEmpty++;
				continue;
			}

			await publishEndpoint.Publish(command, ct);
			published++;
		}

		await storage.UpdateChannelParsingParametersAsync(id, newestId, checkNewPosts, ct);
		await runStorage.AddRunAsync(new ParseChannelRunReport
		{
			ChannelParsingSettingId = id,
			StartedAt = startedAt,
			FinishedAt = timeProvider.GetUtcNow(),
			Status = ParseChannelRunStatus.Success,
			FetchedMessages = allMessages.Count,
			Posts = groupedMessages.Count,
			SkippedByAvoidWords = skippedByAvoidWords,
			SkippedAsAds = skippedAsAds,
			SkippedAsDuplicates = skippedAsDuplicates,
			SkippedEmpty = skippedEmpty,
			Published = published,
			LastMessageId = newestId == 0 ? null : newestId
		}, ct);
		logger.LogInformation("Парсинг канала завершен. Задачи на обработку ({count}) отправлены в очередь.",
			published);
		return true;
	}

	/// <summary>
	/// Записать в журнал проход, который не дошёл до счётчиков: канал не получен или история оборвалась
	/// </summary>
	/// <param name="id"></param>
	/// <param name="startedAt"></param>
	/// <param name="status"></param>
	/// <param name="error"></param>
	/// <param name="ct"></param>
	private Task AddFailedRunAsync(
		Guid id,
		DateTimeOffset startedAt,
		ParseChannelRunStatus status,
		string error,
		CancellationToken ct
	) => runStorage.AddRunAsync(new ParseChannelRunReport
	{
		ChannelParsingSettingId = id,
		StartedAt = startedAt,
		FinishedAt = timeProvider.GetUtcNow(),
		Status = status,
		Error = error
	}, ct);

	/// <summary>
	/// Текст ошибки для журнала: статус Telegram и, если есть, его сообщение
	/// </summary>
	/// <param name="status"></param>
	/// <param name="message"></param>
	/// <returns></returns>
	private static string FormatError(TelegramOperationStatus status, string? message) =>
		string.IsNullOrWhiteSpace(message) ? status.ToString() : $"{status}: {message}";

	/// <summary>
	/// Отбрасывает группы, из которых пост уже создан раньше: повторный парсинг того же сообщения
	/// не должен давать второй пост
	/// </summary>
	/// <param name="id"></param>
	/// <param name="groups"></param>
	/// <param name="channelName"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	private async Task<List<KeyValuePair<long, List<TelegramMessage>>>> SkipAlreadyParsedAsync(
		Guid id,
		List<KeyValuePair<long, List<TelegramMessage>>> groups,
		string channelName,
		CancellationToken ct
	)
	{
		if (groups.Count == 0)
		{
			return groups;
		}

		var candidates = groups
			.Select(g => new ParsedSourceCandidate(
				g.Value.Min(m => m.Id),
				g.Value.Select(m => m.Text).FirstOrDefault(t => !string.IsNullOrEmpty(t))))
			.ToList();
		var parsed = await storage.GetAlreadyParsedSourceIdsAsync(id, candidates, ct);
		if (parsed.Count == 0)
		{
			return groups;
		}

		logger.LogInformation("Канал {ChannelName}: пропущено уже спарсенных постов {Count}", channelName,
			parsed.Count);
		return groups.Where(g => !parsed.Contains(g.Value.Min(m => m.Id))).ToList();
	}

	/// <summary>
	/// Отбрасывает группы (альбомы), где хоть одно сообщение похоже на рекламу, и пишет в лог, сколько групп
	/// отброшено и по каким правилам
	/// </summary>
	/// <param name="groups"></param>
	/// <param name="channelName"></param>
	/// <returns></returns>
	private List<KeyValuePair<long, List<TelegramMessage>>> SkipAdGroups(
		List<KeyValuePair<long, List<TelegramMessage>>> groups,
		string channelName
	)
	{
		List<KeyValuePair<long, List<TelegramMessage>>> result = [];
		Dictionary<string, int> skippedByRule = [];
		foreach (var group in groups)
		{
			var ad = group.Value
				.Select(message => new { message.Id, Detection = AdDetector.Detect(message.Text, message.Entities) })
				.FirstOrDefault(x => x.Detection.IsAd);
			if (ad is not { Detection.Reason: { } rule })
			{
				result.Add(group);
				continue;
			}

			logger.LogDebug("Рекламный пост {MessageId} канала {ChannelName} пропущен по правилу {Rule}",
				ad.Id, channelName, rule);
			skippedByRule[rule] = skippedByRule.GetValueOrDefault(rule) + 1;
		}

		if (result.Count < groups.Count)
		{
			logger.LogInformation("Канал {ChannelName}: отброшено рекламных постов {SkippedCount} ({SkippedByRule})",
				channelName, groups.Count - result.Count,
				string.Join(", ", skippedByRule.Select(x => $"{x.Key}: {x.Value}")));
		}

		return result;
	}

	/// <summary>
	/// Отказ Telegram, который не пройдёт сам: канала нет, аккаунт парсинга в нём забанен или у него нет доступа
	/// </summary>
	/// <param name="status"></param>
	/// <returns></returns>
	private static bool IsSourceUnavailable(TelegramOperationStatus status) =>
		status is TelegramOperationStatus.UsernameNotFound
			or TelegramOperationStatus.ChannelBanned
			or TelegramOperationStatus.AccessDenied;

	/// <summary>
	/// Помечает источник ошибкой и оповещает владельца расписания. Троттлинг оповещения — по источнику, чтобы
	/// сломанный источник не заглушал оповещения о других источниках того же расписания
	/// </summary>
	/// <param name="id"></param>
	/// <param name="parameters"></param>
	/// <param name="status"></param>
	/// <param name="ct"></param>
	private async Task MarkSourceUnavailableAsync(
		Guid id,
		ParseChannelParsingSettings parameters,
		TelegramOperationStatus status,
		CancellationToken ct
	)
	{
		await storage.UpdateErrorStatusAsync(id, ct);
		await alertService.NotifyAsync(parameters.ScheduleId, ScheduleProblem.ParsingFailed,
			$"Источник @{parameters.ChannelName}: канал не найден или недоступен ({status})", throttleKey: id,
			ct: ct);
	}
}

public record ProcessMessage
{
	public required Guid Id { get; init; }
	public required Guid TelegramBotId { get; init; }
	public List<MessageCommand> Messages { get; init; } = [];
}

public class MessageCommand
{
	public int Id { get; init; }
	public bool IsVideo { get; init; }
	public bool IsPhoto { get; init; }
}