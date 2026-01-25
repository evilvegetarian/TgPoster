using MassTransit;
using Microsoft.Extensions.Logging;
using Shared.Telegram;
using TL;

namespace TgPoster.Worker.Domain.UseCases.RepostMessageConsumer;

internal sealed class RepostMessageConsumer(
	IRepostMessageConsumerStorage storage,
	TelegramAuthService authService,
	ILogger<RepostMessageConsumer> logger) : IConsumer<RepostMessageCommand>
{
	public async Task Consume(ConsumeContext<RepostMessageCommand> context)
	{
		var command = context.Message;
		logger.LogInformation("Начал обработку репоста для сообщения {MessageId}", command.MessageId);

		var repostData = await storage.GetRepostDataAsync(command.MessageId, context.CancellationToken);
		if (repostData is null)
		{
			logger.LogWarning("Данные для репоста не найдены для сообщения {MessageId}", command.MessageId);
			return;
		}

		if (repostData.TelegramMessageId is null)
		{
			logger.LogWarning("TelegramMessageId отсутствует для сообщения {MessageId}", command.MessageId);
			return;
		}

		if (repostData.Destinations.Count == 0)
		{
			logger.LogInformation("Нет активных направлений для репоста сообщения {MessageId}", command.MessageId);
			return;
		}

		var client = await authService.GetClientAsync(repostData.TelegramSessionId, context.CancellationToken);

		var resolveResult = await client.Contacts_ResolveUsername(repostData.SourceChannelIdentifier);
		if (resolveResult.Chat is not Channel sourceChannel)
		{
			logger.LogError("Не удалось найти исходный канал: {ChannelName}", repostData.SourceChannelIdentifier);
			return;
		}

		//TODO:Поменять
		/*foreach (var destination in repostData.Destinations)
		{
			try
			{
				var destResolveResult = await client.Contacts_ResolveUsername(destination.ChatIdentifier);
				InputPeer toPeer;

				switch (destResolveResult.Chat)
				{
					case Channel destChannel:
						toPeer = new InputPeerChannel(destChannel.ID, destChannel.access_hash);
						break;
					case ChatBase destChat:
						toPeer = new InputPeerChat(destChat.ID);
						break;
					default:
						logger.LogError("Не удалось определить тип чата для {ChatIdentifier}", destination.ChatIdentifier);
						await storage.CreateRepostLogAsync(
							command.MessageId,
							destination.Id,
							null,
							"Не удалось определить тип чата",
							context.CancellationToken);
						continue;
				}

				var result = await client.Messages_ForwardMessages(
					from_peer: new InputPeerChannel(sourceChannel.ID, sourceChannel.access_hash),
					id: [repostData.TelegramMessageId.Value],
					to_peer: toPeer,
					random_id: [Random.Shared.NextInt64()]
				);

				int? repostedMessageId = null;
				if (result is Updates updates && updates.updates.Length > 0)
				{
					var messageUpdate = updates.updates
						.OfType<UpdateMessageID>()
						.FirstOrDefault();

					if (messageUpdate != null)
					{
						repostedMessageId = messageUpdate.id;
					}
				}

				await storage.CreateRepostLogAsync(
					command.MessageId,
					destination.Id,
					repostedMessageId,
					null,
					context.CancellationToken);

				logger.LogInformation(
					"Сообщение {MessageId} успешно репостнуто в {ChatIdentifier}",
					command.MessageId,
					destination.ChatIdentifier);
			}
			catch (Exception e)
			{
				logger.LogError(e, "Ошибка при репосте в {ChatIdentifier}", destination.ChatIdentifier);
				await storage.CreateRepostLogAsync(
					command.MessageId,
					destination.Id,
					null,
					e.Message,
					context.CancellationToken);
			}
		}*/

		logger.LogInformation("Завершена обработка репоста для сообщения {MessageId}", command.MessageId);
	}
}

public sealed record RepostMessageCommand
{
	public required Guid MessageId { get; init; }
	public required Guid ScheduleId { get; init; }
}
