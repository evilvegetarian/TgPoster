using MassTransit;
using Microsoft.Extensions.Logging;
using Shared.Enums;
using TgPoster.Telegram.Abstractions;
using TgPoster.Telegram.Models;

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

		if (repostData.Destinations.Count == 0)
		{
			logger.LogInformation("Нет активных направлений для репоста сообщения {MessageId}", command.MessageId);
			return;
		}

		if (repostData.TelegramMessageId is null)
		{
			logger.LogWarning("TelegramMessageId отсутствует для сообщения {MessageId}", command.MessageId);
			await LogForAllDestinationsAsync(
				repostData,
				command.MessageId,
				RepostLogReason.MessageNotPublished,
				"Сообщение ещё не опубликовано в канале-источнике",
				ct);
			return;
		}

		var sessionId = repostData.TelegramSessionId;

		var dialogsResult = await tgMessages.GetAllDialogsAsync(sessionId, ct);
		if (!dialogsResult.IsSuccess)
		{
			logger.LogError("Не удалось получить диалоги: {Status} {Error}",
				dialogsResult.Status, dialogsResult.ErrorMessage);
			await LogForAllDestinationsAsync(
				repostData,
				command.MessageId,
				RepostLogReason.DialogsUnavailable,
				$"Не удалось получить диалоги сессии ({dialogsResult.Status}): {dialogsResult.ErrorMessage}",
				ct);
			return;
		}

		var resolveResult = await tgMessages.ResolveChannelAsync(
			sessionId, repostData.SourceChannelIdentifier.Replace("@", ""), ct);
		if (!resolveResult.IsSuccess)
		{
			logger.LogError("Не удалось найти исходный канал: {ChannelName} ({Status})",
				repostData.SourceChannelIdentifier, resolveResult.Status);
			await LogForAllDestinationsAsync(
				repostData,
				command.MessageId,
				RepostLogReason.SourceChannelNotResolved,
				$"Канал-источник {repostData.SourceChannelIdentifier} не найден сессией ({resolveResult.Status})",
				ct);
			return;
		}

		var sourceChannel = resolveResult.Value!;

		var dialogs = dialogsResult.Value!
			.DistinctBy(c => c.Id)
			.ToDictionary(c => c.Id);

		foreach (var dest in repostData.Destinations)
		{
			if (!dialogs.TryGetValue(dest.ChatIdentifier, out var destination))
			{
				logger.LogWarning(
					"Целевой канал {ChatId} отсутствует в диалогах сессии {SessionId}",
					dest.ChatIdentifier, sessionId);
				await WriteLogAsync(new RepostLogEntry
				{
					MessageId = command.MessageId,
					RepostDestinationId = dest.Id,
					Status = RepostStatus.Failed,
					Reason = RepostLogReason.DestinationNotAvailable,
					Error = "Целевой канал отсутствует в диалогах сессии — аккаунт в нём не состоит"
				}, ct);
				continue;
			}

			var skipReason = await GetSkipReasonAsync(dest, ct);
			if (skipReason != RepostLogReason.None)
			{
				logger.LogInformation(
					"Репост сообщения {MessageId} в {ChatId} пропущен: {Reason}",
					command.MessageId, dest.ChatIdentifier, skipReason);
				await WriteLogAsync(new RepostLogEntry
				{
					MessageId = command.MessageId,
					RepostDestinationId = dest.Id,
					Status = RepostStatus.Skipped,
					Reason = skipReason,
					Error = DescribeSkip(skipReason, dest)
				}, ct);
				continue;
			}

			if (dest.DelayMaxSeconds > 0)
			{
				var delaySec = Random.Shared.Next(dest.DelayMinSeconds, dest.DelayMaxSeconds + 1);
				logger.LogDebug(
					"Задержка {Delay} сек. перед репостом в {ChatId}",
					delaySec, destination.Id);
				await Task.Delay(TimeSpan.FromSeconds(delaySec), ct);
			}

			var forwardResult = await tgMessages.ForwardMessageAsync(
				sessionId,
				sourceChannel.Peer,
				destination.Peer,
				repostData.TelegramMessageId.Value,
				ct);

			if (forwardResult.IsSuccess)
			{
				logger.LogInformation(
					"Сообщение {MessageId} репостнуто в {ChatId}", command.MessageId, dest.ChatIdentifier);
				await WriteLogAsync(new RepostLogEntry
				{
					MessageId = command.MessageId,
					RepostDestinationId = dest.Id,
					Status = RepostStatus.Success,
					Reason = RepostLogReason.None,
					TelegramMessageId = forwardResult.Value
				}, ct);

				continue;
			}

			var reason = RepostLogReason.ForwardFailed;
			if (forwardResult.Status == TelegramOperationStatus.ChannelBanned)
			{
				logger.LogWarning(
					"Аккаунт заблокирован в канале {ChatId}: {Error}. Направление репоста отключено",
					destination.Id, forwardResult.ErrorMessage);
				await storage.UpdateDestinationStatusAsync(dest.Id, ChatStatus.Banned, false, ct);
				reason = RepostLogReason.Banned;
			}
			else
			{
				logger.LogError("Ошибка при репосте в {ChatIdentifier}: {Status} {Error}",
					destination.Id, forwardResult.Status, forwardResult.ErrorMessage);
			}

			await WriteLogAsync(new RepostLogEntry
			{
				MessageId = command.MessageId,
				RepostDestinationId = dest.Id,
				Status = RepostStatus.Failed,
				Reason = reason,
				Error = forwardResult.ErrorMessage ?? forwardResult.Status.ToString()
			}, ct);
		}
	}

	/// <summary>
	///     Определяет, нужно ли пропустить репост в конкретный канал
	/// </summary>
	/// <param name="dest">Направление репоста с настройками рандомизации</param>
	/// <param name="ct">Токен отмены</param>
	/// <returns>Причина пропуска или <see cref="RepostLogReason.None" />, если репостить нужно</returns>
	private async Task<RepostLogReason> GetSkipReasonAsync(RepostDestinationDataDto dest, CancellationToken ct)
	{
		var counter = await storage.IncrementRepostCounterAsync(dest.Id, ct);

		if (dest.RepostEveryNth > 1 && counter % dest.RepostEveryNth != 0)
		{
			return RepostLogReason.EveryNth;
		}

		if (dest.SkipProbability > 0 && Random.Shared.Next(100) < dest.SkipProbability)
		{
			return RepostLogReason.SkipProbability;
		}

		if (dest.MaxRepostsPerDay.HasValue)
		{
			var todayCount = await storage.GetTodayRepostCountAsync(dest.Id, ct);
			if (todayCount >= dest.MaxRepostsPerDay.Value)
			{
				return RepostLogReason.DailyLimit;
			}
		}

		return RepostLogReason.None;
	}

	/// <summary>
	///     Собирает пояснение к пропуску, чтобы в журнале было видно, какая именно настройка сработала
	/// </summary>
	/// <param name="reason">Причина пропуска</param>
	/// <param name="dest">Направление репоста с настройками рандомизации</param>
	/// <returns>Текст пояснения</returns>
	private static string DescribeSkip(RepostLogReason reason, RepostDestinationDataDto dest) => reason switch
	{
		RepostLogReason.EveryNth => $"Репостится каждое {dest.RepostEveryNth}-е сообщение",
		RepostLogReason.SkipProbability => $"Случайный пропуск с вероятностью {dest.SkipProbability}%",
		RepostLogReason.DailyLimit => $"Достигнут дневной лимит {dest.MaxRepostsPerDay} репостов",
		_ => reason.ToString()
	};

	private Task WriteLogAsync(RepostLogEntry entry, CancellationToken ct) =>
		storage.CreateRepostLogsAsync([entry], ct);

	private Task LogForAllDestinationsAsync(
		RepostDataDto repostData,
		Guid messageId,
		RepostLogReason reason,
		string error,
		CancellationToken ct
	)
	{
		var entries = repostData.Destinations
			.Select(dest => new RepostLogEntry
			{
				MessageId = messageId,
				RepostDestinationId = dest.Id,
				Status = RepostStatus.Failed,
				Reason = reason,
				Error = error
			})
			.ToList();

		return storage.CreateRepostLogsAsync(entries, ct);
	}
}
