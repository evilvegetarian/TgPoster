using MassTransit;
using Microsoft.Extensions.Logging;
using Shared.Enums;
using TgPoster.Telegram;

namespace TgPoster.Worker.Domain.UseCases.RepostMessageConsumer;

internal sealed class RepostMessageConsumer(
	IRepostMessageConsumerStorage storage,
	ITelegramMessageService tgMessages,
	ILogger<RepostMessageConsumer> logger)
	: IConsumer<RepostMessageCommand>
{
	public async Task Consume(ConsumeContext<RepostMessageCommand> context)
	{
		var command = context.Message;
		var ct = context.CancellationToken;

		var repostData = await storage.GetRepostDataAsync(command.MessageId, command.RepostSettingsId, ct);
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

		var sessionId = repostData.TelegramSessionId;

		var dialogsResult = await tgMessages.GetAllDialogsAsync(sessionId, ct);
		if (!dialogsResult.IsSuccess)
		{
			logger.LogError("Не удалось получить диалоги: {Status} {Error}",
				dialogsResult.Status, dialogsResult.ErrorMessage);
			return;
		}

		var resolveResult = await tgMessages.ResolveChannelAsync(
			sessionId, repostData.SourceChannelIdentifier.Replace("@", ""), ct);
		if (!resolveResult.IsSuccess)
		{
			logger.LogError("Не удалось найти исходный канал: {ChannelName} ({Status})",
				repostData.SourceChannelIdentifier, resolveResult.Status);
			return;
		}

		var sourceChannel = resolveResult.Value!;

		var destinationIds = repostData.Destinations.Select(d => d.ChatIdentifier).ToHashSet();
		var destinations = dialogsResult.Value!
			.Where(c => destinationIds.Contains(c.Id))
			.ToList();

		foreach (var destination in destinations)
		{
			var dest = repostData.Destinations.FirstOrDefault(x => x.ChatIdentifier == destination.Id);
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
					delaySec, destination.Id);
				await Task.Delay(TimeSpan.FromSeconds(delaySec), ct);
			}

			var forwardResult = await tgMessages.ForwardMessageAsync(
				sessionId,
				from: sourceChannel.Peer,
				to: destination.Peer,
				messageId: repostData.TelegramMessageId.Value,
				ct: ct);

			if (forwardResult.IsSuccess)
			{
				await storage.CreateRepostLogAsync(
					command.MessageId,
					dest.Id,
					forwardResult.Value,
					null,
					ct);

				logger.LogInformation(
					"Сообщение {MessageId} успешно репостнуто в {ChatIdentifier}",
					command.MessageId,
					destination.Id);
				continue;
			}

			if (forwardResult.Status == TelegramOperationStatus.ChannelBanned)
			{
				logger.LogWarning("Аккаунт заблокирован в канале {ChatId}: {Error}",
					destination.Id, forwardResult.ErrorMessage);
				await storage.UpdateDestinationStatusAsync(dest.Id, ChatStatus.Banned, ct);
			}
			else
			{
				logger.LogError("Ошибка при репосте в {ChatIdentifier}: {Status} {Error}",
					destination.Id, forwardResult.Status, forwardResult.ErrorMessage);
			}

			await storage.CreateRepostLogAsync(
				command.MessageId,
				dest.Id,
				null,
				forwardResult.ErrorMessage,
				ct);
		}
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
