using Microsoft.Extensions.Logging;
using TL;
using WTelegram;
using MassTransit;
using Message = TL.Message;

namespace TgPoster.Worker.Domain.UseCases.ParseChannel;

internal class ParseChannelUseCase(
	IParseChannelUseCaseStorage storage,
	IPublishEndpoint publishEndpoint,
	Client client,
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

		var resolveResult = await client.Contacts_ResolveUsername(channelName);
		if (resolveResult.Chat is not Channel channel)
		{
			logger.LogError("Не удалось найти канал по имени: {ChannelName}", channelName);
			await storage.UpdateChannelParsingParametersAsync(id, lastParseId ?? 0, checkNewPosts,
				ct);
			return;
		}

		List<Message> allMessages = [];
		var tempLastParseId = 0;
		const int limit = 100;
		var offset = lastParseId ?? 0;
		while (true)
		{
			var history = await client.Messages_GetHistory(
				new InputPeerChannel(channel.ID, channel.access_hash),
				limit: limit,
				offset_date: toDate ?? DateTime.Now,
				offset_id: offset
			);

			var messageFiltered = history.Messages
				.Where(x => fromDate is null || x.Date >= fromDate)
				.OfType<Message>()
				.Where(x => lastParseId is null || x.ID > lastParseId)
				.ToList();

			allMessages.AddRange(messageFiltered);

			if (history.Messages.Length is not 0)
			{
				var maxId = history.Messages.Max(x => x.ID);
				if (tempLastParseId < maxId) tempLastParseId = maxId;
			}

			if (history.Messages.Length is 0
			    || history.Messages.Any(x => x.ID < lastParseId)
			    || history.Messages.Any(x => x.Date < fromDate))
			{
				break;
			}

			offset = history.Messages.Last().ID;
		}

		var groupedMessages = new Dictionary<long, List<Message>>();
		long i = 1;
		foreach (var message in allMessages.OrderBy(m => m.ID))
		{
			if (message.grouped_id != 0)
			{
				if (!groupedMessages.ContainsKey(message.grouped_id))
					groupedMessages[message.grouped_id] = [];
				groupedMessages[message.grouped_id].Add(message);
			}
			else
			{
				groupedMessages[i++] = [message];
			}
		}

		var validGroups = groupedMessages
			.Where(group => !group.Value.Any(
				msg => msg.message != null
				       && avoidWords.Any(word => msg.message.Contains(word, StringComparison.OrdinalIgnoreCase))
			)).ToList();

		logger.LogInformation("Найдено {Count} новых постов для обработки.", validGroups.Count);

		foreach (var group in validGroups)
		{
			var command = new ProcessMessageCommand
			{
				Id = id,
				TelegramBotId = telegramBotId,
				Messages = group.Value.Select(m => new MessageCommand
				{
					Id = m.ID,
					IsPhoto = m.media is MessageMediaPhoto ,
					IsVideo = m.media is MessageMediaDocument 
				}).ToList()
			};

			if (group.Value.All(p => p.message is null && p.media is null))
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

public record ProcessMessageCommand
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