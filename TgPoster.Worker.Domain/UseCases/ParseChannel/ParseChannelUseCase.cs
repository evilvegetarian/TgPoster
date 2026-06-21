using MassTransit;
using Microsoft.Extensions.Logging;
using TgPoster.Telegram.Abstractions;
using TgPoster.Telegram.Models;

namespace TgPoster.Worker.Domain.UseCases.ParseChannel;

internal class ParseChannelUseCase(
	IParseChannelUseCaseStorage storage,
	IPublishEndpoint publishEndpoint,
	ITelegramMessageService tgMessages,
	ILogger<ParseChannelUseCase> logger)
{
	public async Task Handle(Guid id, CancellationToken ct)
	{
		var parameters = await storage.GetChannelParsingParametersAsync(id, ct);
		if (parameters is null)
		{
			logger.LogError("Параметров нет, интересно почему..... Id: {id}", id);
			return;
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
			await storage.UpdateChannelParsingParametersAsync(id, lastParseId ?? 0, checkNewPosts, ct);
			return;
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
				await storage.UpdateChannelParsingParametersAsync(id, tempLastParseId, checkNewPosts, ct);
				return;
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