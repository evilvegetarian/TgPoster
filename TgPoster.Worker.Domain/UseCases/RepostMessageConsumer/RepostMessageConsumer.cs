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
			logger.LogDebug("Нет активных направлений для репоста сообщения {MessageId}", command.MessageId);
			return;
		}

		// Бан аккаунта в чате уже зафиксирован прошлой попыткой: за тем же отказом в Telegram не ходим
		var destinations = await DisableBannedDestinationsAsync(repostData.Destinations, command.MessageId, ct);
		if (destinations.Count == 0)
		{
			return;
		}

		if (repostData.TelegramMessageId is null)
		{
			logger.LogWarning("TelegramMessageId отсутствует для сообщения {MessageId}", command.MessageId);
			await LogForAllDestinationsAsync(
				destinations,
				command.MessageId,
				RepostLogReason.MessageNotPublished,
				"Сообщение ещё не опубликовано в канале-источнике",
				ct);
			return;
		}

		var sessionId = repostData.TelegramSessionId;

		// Не GetAllDialogs: диалоги читаются страницами, активный чат между страницами уезжает вверх
		// и выпадает из выборки, а направление из-за этого ошибочно отключалось бы как покинутое
		var joinedResult = await tgMessages.GetChatMembershipAsync(sessionId, ct, true);
		if (!joinedResult.IsSuccess)
		{
			logger.LogError("Не удалось получить чаты сессии: {Status} {Error}",
				joinedResult.Status, joinedResult.ErrorMessage);
			await LogForAllDestinationsAsync(
				destinations,
				command.MessageId,
				RepostLogReason.DialogsUnavailable,
				$"Не удалось получить чаты сессии ({joinedResult.Status}): {joinedResult.ErrorMessage}",
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
				destinations,
				command.MessageId,
				RepostLogReason.SourceChannelNotResolved,
				$"Канал-источник {repostData.SourceChannelIdentifier} не найден сессией ({resolveResult.Status})",
				ct);
			return;
		}

		var sourceChannel = resolveResult.Value!;

		var membership = joinedResult.Value!;

		foreach (var dest in destinations)
		{
			// Telegram сам отдал чат недоступным: аккаунт забанен или исключён, а не вышел сам
			if (membership.ForbiddenChatIds.Contains(dest.ChatIdentifier))
			{
				await MarkAccountBannedAsync(dest, sessionId, command.MessageId, "CHANNEL_FORBIDDEN", ct);
				continue;
			}

			if (!membership.Chats.TryGetValue(dest.ChatIdentifier, out var destination))
			{
				// Аккаунт вышел из чата или его удалили: без отключения направление падало бы на каждом посте.
				// При включении направления аккаунт вступает в чат заново
				logger.LogWarning(
					"Аккаунт не состоит в целевом чате {ChatId}. Направление репоста {DestinationId} отключено",
					dest.ChatIdentifier, dest.Id);
				await storage.UpdateDestinationStatusAsync(dest.Id, ChatStatus.Left, false, ct);
				await WriteLogAsync(new RepostLogEntry
				{
					MessageId = command.MessageId,
					RepostDestinationId = dest.Id,
					Status = RepostStatus.Failed,
					Reason = RepostLogReason.DestinationNotAvailable,
					Error = "Аккаунт не состоит в целевом чате, направление отключено"
				}, ct);
				continue;
			}

			// Ограничение наложено лично на аккаунт: пересылка упала бы с той же CHAT_WRITE_FORBIDDEN, что и
			// при общем запрете чата, и ушла бы в Discover как свойство чата. Записываем за аккаунтом заранее
			if (destination.IsAccountRestricted)
			{
				await MarkAccountBannedAsync(
					dest, sessionId, command.MessageId, "Аккаунт ограничен в чате администратором", ct);
				continue;
			}

			var skipReason = await GetSkipReasonAsync(dest, ct);
			if (skipReason != RepostLogReason.None)
			{
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

			if (forwardResult.Status == TelegramOperationStatus.ChannelBanned)
			{
				await MarkAccountBannedAsync(
					dest,
					sessionId,
					command.MessageId,
					forwardResult.ErrorMessage ?? forwardResult.Status.ToString(),
					ct);
				continue;
			}

			RepostLogReason reason;
			switch (forwardResult.Status)
			{
				// Запрет общий для чата, а не бан аккаунта: в Discover, а не в реестр банов сессии
				case TelegramOperationStatus.NoWritePermission:
					logger.LogWarning(
						"В канал {ChatId} нельзя писать: {Error}. Направление репоста отключено",
						destination.Id, forwardResult.ErrorMessage);
					await storage.DisableForChatRestrictionAsync(dest.Id, false, null, ct);
					reason = RepostLogReason.NoWritePermission;
					break;

				case TelegramOperationStatus.NoMediaPermission:
					logger.LogWarning(
						"В канал {ChatId} нельзя отправлять медиа: {Error}. Направление репоста отключено",
						destination.Id, forwardResult.ErrorMessage);
					await storage.DisableForChatRestrictionAsync(dest.Id, null, false, ct);
					reason = RepostLogReason.NoMediaPermission;
					break;

				case TelegramOperationStatus.TopicClosed:
					reason = RepostLogReason.TopicClosed;
					break;

				default:
					logger.LogError("Ошибка при репосте в {ChatIdentifier}: {Status} {Error}",
						destination.Id, forwardResult.Status, forwardResult.ErrorMessage);
					reason = RepostLogReason.ForwardFailed;
					break;
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

	/// <summary>
	/// Записывает бан аккаунта в чате и отключает все направления этой сессии в этот чат, в том числе
	/// из других настроек репоста
	/// </summary>
	/// <param name="dest"></param>
	/// <param name="sessionId"></param>
	/// <param name="messageId"></param>
	/// <param name="error"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	private async Task MarkAccountBannedAsync(
		RepostDestinationDataDto dest,
		Guid sessionId,
		Guid messageId,
		string error,
		CancellationToken ct
	)
	{
		logger.LogWarning(
			"Аккаунт заблокирован в канале {ChatId}: {Error}. Направления репоста этой сессии в канал отключены",
			dest.ChatIdentifier, error);
		await storage.MarkAccountBannedAsync(sessionId, dest.ChatIdentifier, error, ct);
		await WriteLogAsync(new RepostLogEntry
		{
			MessageId = messageId,
			RepostDestinationId = dest.Id,
			Status = RepostStatus.Failed,
			Reason = RepostLogReason.Banned,
			Error = error
		}, ct);
	}

	/// <summary>
	/// Отключает направления в чаты, где у аккаунта зафиксирован бан, и возвращает остальные. Бан снимает
	/// ручное включение направления: оно заново проверяет доступ аккаунта к чату
	/// </summary>
	/// <param name="destinations"></param>
	/// <param name="messageId"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	private async Task<List<RepostDestinationDataDto>> DisableBannedDestinationsAsync(
		List<RepostDestinationDataDto> destinations,
		Guid messageId,
		CancellationToken ct
	)
	{
		foreach (var dest in destinations.Where(d => d.IsAccountBanned))
		{
			logger.LogWarning(
				"Аккаунт заблокирован в канале {ChatId} по данным прошлой попытки. Направление репоста {DestinationId} отключено",
				dest.ChatIdentifier, dest.Id);
			await storage.DeactivateDestinationAsync(dest.Id, ct);
			await WriteLogAsync(new RepostLogEntry
			{
				MessageId = messageId,
				RepostDestinationId = dest.Id,
				Status = RepostStatus.Failed,
				Reason = RepostLogReason.Banned,
				Error = "Аккаунт заблокирован в канале по данным прошлой попытки, направление отключено"
			}, ct);
		}

		return destinations.Where(d => !d.IsAccountBanned).ToList();
	}

	private Task WriteLogAsync(RepostLogEntry entry, CancellationToken ct) =>
		storage.CreateRepostLogsAsync([entry], ct);

	private Task LogForAllDestinationsAsync(
		List<RepostDestinationDataDto> destinations,
		Guid messageId,
		RepostLogReason reason,
		string error,
		CancellationToken ct
	)
	{
		var entries = destinations
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
