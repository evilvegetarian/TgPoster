using MassTransit;
using Microsoft.Extensions.Logging;
using Shared.Enums;
using Shared.Telegram;
using TL;

namespace TgPoster.Worker.Domain.UseCases.RepostMessageConsumer;

internal sealed class RepostMessageConsumer(
	IRepostMessageConsumerStorage storage,
	TelegramAuthService authService,
	ILogger<RepostMessageConsumer> logger)
	: IConsumer<RepostMessageCommand>
{
	public async Task Consume(ConsumeContext<RepostMessageCommand> context)
	{
		var command = context.Message;
		logger.LogInformation("Начал обработку репоста для сообщения {MessageId}", command.MessageId);

		var repostData = await storage.GetRepostDataAsync(command.MessageId, command.RepostSettingsId, context.CancellationToken);
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
		var dialogs = await client.Messages_GetAllDialogs();
		var resolveResult = await client.Contacts_ResolveUsername(repostData.SourceChannelIdentifier.Replace("@",""));
		if (resolveResult.Chat is not Channel sourceChannel)
		{
			logger.LogError("Не удалось найти исходный канал: {ChannelName}", repostData.SourceChannelIdentifier);
			return;
		}

		var destinations = dialogs.chats
			.Where(x => repostData.Destinations
				.Select(dto => dto.ChatIdentifier)
				.Contains(x.Key))
			.Select(x=>x.Value)
			.ToList();
		foreach (var destination in destinations)
		{
			var dest=repostData.Destinations.FirstOrDefault(x => x.ChatIdentifier==destination.ID);
			if (dest is null)
			{
				continue;
			}

			if (!await ShouldRepostToDestinationAsync(dest, command.MessageId, context.CancellationToken))
			{
				continue;
			}

			if (dest.DelayMaxSeconds > 0)
			{
				var delaySec = Random.Shared.Next(dest.DelayMinSeconds, dest.DelayMaxSeconds + 1);
				logger.LogInformation(
					"Задержка {Delay} сек. перед репостом в {ChatId}",
					delaySec, destination.ID);
				await Task.Delay(TimeSpan.FromSeconds(delaySec), context.CancellationToken);
			}

			try
			{
				var result = await client.Messages_ForwardMessages(
					from_peer: new InputPeerChannel(sourceChannel.ID, sourceChannel.access_hash),
					id: [repostData.TelegramMessageId.Value],
					to_peer: destination.ToInputPeer(),
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
					dest.Id,
					repostedMessageId,
					null,
					context.CancellationToken);

				logger.LogInformation(
					"Сообщение {MessageId} успешно репостнуто в {ChatIdentifier}",
					command.MessageId,
					destination.ID);
			}
			catch (RpcException rpcEx) when (
				rpcEx.Message is "CHANNEL_PRIVATE" or "USER_BANNED_IN_CHANNEL"
					or "CHAT_WRITE_FORBIDDEN" or "CHAT_RESTRICTED" or "CHAT_SEND_PLAIN_FORBIDDEN")
			{
				logger.LogWarning("Аккаунт заблокирован в канале {ChatId}: {Error}", destination.ID, rpcEx.Message);
				await storage.UpdateDestinationStatusAsync(dest.Id, ChatStatus.Banned, context.CancellationToken);
				await storage.CreateRepostLogAsync(
					command.MessageId,
					dest.Id,
					null,
					rpcEx.Message,
					context.CancellationToken);
			}
			catch (Exception e)
			{
				logger.LogError(e, "Ошибка при репосте в {ChatIdentifier}", destination.ID);
				await storage.CreateRepostLogAsync(
					command.MessageId,
					dest.Id,
					null,
					e.Message,
					context.CancellationToken);
			}
		}

		logger.LogInformation("Завершена обработка репоста для сообщения {MessageId}", command.MessageId);
	}

	private async Task<bool> ShouldRepostToDestinationAsync(
		RepostDestinationDataDto dest, Guid messageId, CancellationToken ct)
	{
		var counter = await storage.IncrementRepostCounterAsync(dest.Id, ct);

		if (dest.RepostEveryNth > 1 && counter % dest.RepostEveryNth != 0)
		{
			logger.LogInformation(
				"Репост пропущен для канала {DestId}: счётчик {Counter}, каждое {N}-е",
				dest.Id, counter, dest.RepostEveryNth);
			return false;
		}

		if (dest.SkipProbability > 0 && Random.Shared.Next(100) < dest.SkipProbability)
		{
			logger.LogInformation(
				"Репост случайно пропущен для канала {DestId}: вероятность {Probability}%",
				dest.Id, dest.SkipProbability);
			return false;
		}

		if (dest.MaxRepostsPerDay.HasValue)
		{
			var todayCount = await storage.GetTodayRepostCountAsync(dest.Id, ct);
			if (todayCount >= dest.MaxRepostsPerDay.Value)
			{
				logger.LogInformation(
					"Репост пропущен для канала {DestId}: дневной лимит {Limit} исчерпан ({Count})",
					dest.Id, dest.MaxRepostsPerDay.Value, todayCount);
				return false;
			}
		}

		return true;
	}
}
