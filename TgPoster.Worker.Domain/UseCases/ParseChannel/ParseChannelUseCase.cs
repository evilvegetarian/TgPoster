using MassTransit;
using Microsoft.Extensions.Logging;
using TgPoster.Telegram.Abstractions;
using TgPoster.Telegram.Models;
using TgPoster.Worker.Domain.Services.AdDetection;
using TgPoster.Worker.Domain.Services.ScheduleAlerts;

namespace TgPoster.Worker.Domain.UseCases.ParseChannel;

internal class ParseChannelUseCase(
	IParseChannelUseCaseStorage storage,
	IPublishEndpoint publishEndpoint,
	ITelegramMessageService tgMessages,
	IScheduleAlertService alertService,
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
				return false;
			}

			await storage.UpdateChannelParsingParametersAsync(id, lastParseId ?? 0, checkNewPosts, ct);
			return true;
		}

		var channel = resolveResult.Value!;
		List<TelegramMessage> allMessages = [];
		var tempLastParseId = 0;
		const int limit = 100;
		var offset = lastParseId ?? 0;
		while (true)
		{
			var historyResult = await tgMessages.GetHistoryAsync(
				sessionId,
				channel.Peer,
				limit,
				offsetDate: toDate ?? DateTime.Now,
				offsetId: offset,
				ct: ct);

			if (!historyResult.IsSuccess)
			{
				logger.LogError("Ошибка при получении истории канала {ChannelName}: {Status} {Error}",
					channelName, historyResult.Status, historyResult.ErrorMessage);
				if (IsSourceUnavailable(historyResult.Status))
				{
					await MarkSourceUnavailableAsync(id, parameters, historyResult.Status, ct);
					return false;
				}

				await storage.UpdateChannelParsingParametersAsync(id, tempLastParseId, checkNewPosts, ct);
				return true;
			}

			var history = historyResult.Value!;

			var messageFiltered = history.Messages
				.Where(x => fromDate is null || x.Date >= fromDate)
				.Where(x => lastParseId is null || x.Id > lastParseId)
				.ToList();

			allMessages.AddRange(messageFiltered);

			if (history.Messages.Count is not 0)
			{
				var maxId = history.Messages.Max(x => x.Id);
				if (tempLastParseId < maxId)
				{
					tempLastParseId = maxId;
				}
			}

			if (history.Messages.Count is 0
			    || history.Messages.Any(x => x.Id < lastParseId)
			    || history.Messages.Any(x => x.Date < fromDate))
			{
				break;
			}

			offset = history.Messages[^1].Id;
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

		if (parameters.SkipAds)
		{
			validGroups = SkipAdGroups(validGroups, channelName);
		}

		logger.LogDebug("Найдено {Count} новых постов для обработки.", validGroups.Count);

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
				continue;
			}

			await publishEndpoint.Publish(command, ct);
		}

		await storage.UpdateChannelParsingParametersAsync(id, tempLastParseId, checkNewPosts, ct);
		logger.LogInformation("Парсинг канала завершен. Задачи на обработку ({count}) отправлены в очередь.",
			validGroups.Count);
		return true;
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