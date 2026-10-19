using MassTransit;
using Microsoft.Extensions.Logging;
using Shared.Enums;
using Shared.Telegram;
using TgPoster.Telegram.Abstractions;
using TgPoster.Telegram.Models;
using TgPoster.Worker.Domain.ConfigModels;

namespace TgPoster.Worker.Domain.UseCases.ImportRepostDestinations;

/// <summary>
///     Обрабатывает задание на массовое добавление целевых каналов: резолвит и вступает
///     в каналы по одному с паузами, чтобы Telegram не ограничил аккаунт.
/// </summary>
internal sealed class ImportRepostDestinationsConsumer(
	IImportRepostDestinationsStorage storage,
	ITelegramChatService chatService,
	RepostImportOptions options,
	ILogger<ImportRepostDestinationsConsumer> logger)
	: IConsumer<ImportRepostDestinationsContract>
{
	private const string AccountRestrictedError = "Аккаунт ограничен в канале администратором";

	public async Task Consume(ConsumeContext<ImportRepostDestinationsContract> context)
	{
		var jobId = context.Message.JobId;
		var ct = context.CancellationToken;

		var job = await storage.GetJobAsync(jobId, ct);
		if (job is null)
		{
			logger.LogWarning("Задание на добавление каналов {JobId} не найдено", jobId);
			return;
		}

		if (job.Status is RepostImportStatus.Completed or RepostImportStatus.Failed)
		{
			return;
		}

		if (IsSessionRestricted(job.SessionFloodWaitUntil))
		{
			logger.LogInformation(
				"Задание {JobId} отложено: сессия ограничена до {FloodWaitUntil}",
				jobId, job.SessionFloodWaitUntil);
			await storage.SetJobStatusAsync(jobId, RepostImportStatus.CooldownWait,
				"Telegram ограничил сессию, обработка продолжится позже", ct);

			return;
		}

		var pendingItems = await storage.GetPendingItemsAsync(jobId, ct);
		if (pendingItems.Count == 0)
		{
			await storage.SetJobStatusAsync(jobId, RepostImportStatus.Completed, null, ct);

			return;
		}

		await storage.SetJobStatusAsync(jobId, RepostImportStatus.InProgress, null, ct);

		// За проход берём ограниченную пачку: с паузами между каналами задание на сотни
		// каналов иначе висит в одном Consume часами
		var items = pendingItems.Take(options.MaxItemsPerRun).ToList();

		var existingChatIds = (await storage.GetExistingChatIdsAsync(job.RepostSettingsId, ct)).ToHashSet();
		var needDelay = false;

		foreach (var item in items)
		{
			// Этот аккаунт уже получил отказ в канале. Другой аккаунт туда, возможно, напишет, а этот — нет
			if (item.IsAccountBanned)
			{
				await storage.UpdateItemAsync(item.ItemId, AddDestinationOutcome.NotResolved, null,
					"Аккаунт заблокирован в канале по данным прошлой попытки", ct);
				continue;
			}

			// Права могли обновиться, пока задание стояло в очереди: лишний раз в Telegram не ходим
			var knownRestriction = GetKnownRestriction(item);
			if (knownRestriction != null)
			{
				await storage.UpdateItemAsync(item.ItemId, knownRestriction.Value.Outcome, null,
					knownRestriction.Value.Error, ct);
				continue;
			}

			var identifier = BuildIdentifier(item);
			if (identifier == null)
			{
				await storage.UpdateItemAsync(item.ItemId, AddDestinationOutcome.NotResolved, null,
					"У канала нет ни username, ни инвайт-ссылки", ct);
				continue;
			}

			if (needDelay)
			{
				await DelayBeforeNextChannelAsync(ct);
			}

			needDelay = true;

			var chatResult = await chatService.TryGetChatInfoAsync(job.TelegramSessionId, identifier, job.AutoJoin);

			if (!chatResult.IsSuccess)
			{
				if (chatResult.Status is TelegramOperationStatus.FloodWait
				    or TelegramOperationStatus.SpamRestricted)
				{
					await HandleRateLimitAsync(job, item, chatResult, ct);

					return;
				}

				if (chatResult.Status is TelegramOperationStatus.ChannelsLimitReached)
				{
					await StopOnChannelsLimitAsync(job, item, chatResult.ErrorMessage, ct);

					return;
				}

				// Бан именно этого аккаунта: записываем за ним, а не в общие данные канала
				if (chatResult.Status is TelegramOperationStatus.ChannelBanned && item.TelegramId is { } bannedChatId)
				{
					await storage.AddAccountBanAsync(job.TelegramSessionId, bannedChatId, chatResult.ErrorMessage, ct);
				}

				await storage.UpdateItemAsync(item.ItemId, AddDestinationOutcome.NotResolved, null,
					chatResult.ErrorMessage, ct);
				continue;
			}

			var info = chatResult.Value!;
			var chatType = info.IsChannel ? ChatType.Channel
				: info.IsGroup ? ChatType.Group
				: ChatType.Unknown;

			// В Discover Telegram-id мог быть пустым или устаревшим — фиксируем актуальные данные и общие права
			// участника. Личное ограничение аккаунта другие аккаунты не касается и идёт в реестр банов сессии
			await storage.UpdateDiscoveredChannelAsync(
				item.DiscoveredChannelId,
				info.Id,
				info.Title,
				info.Username,
				chatType,
				info.MembersCanSendMessages,
				info.MembersCanSendMedia,
				ct);

			if (info.IsAccountRestricted)
			{
				await storage.AddAccountBanAsync(job.TelegramSessionId, info.Id, AccountRestrictedError, ct);
			}

			// Повторная проверка уже по фактическому id из Telegram
			var skipOutcome = GetSkipOutcome(info.Id, job.SourceChannelId, existingChatIds);
			if (skipOutcome != null)
			{
				await storage.UpdateItemAsync(item.ItemId, skipOutcome.Value, null, null, ct);
				continue;
			}

			var restrictionError = info.IsAccountRestricted ? AccountRestrictedError : null;

			if (!info.CanSendMessages)
			{
				await storage.UpdateItemAsync(item.ItemId, AddDestinationOutcome.NoWritePermission, null,
					restrictionError, ct);
				continue;
			}

			if (!info.CanSendMedia)
			{
				await storage.UpdateItemAsync(item.ItemId, AddDestinationOutcome.NoMediaPermission, null,
					restrictionError, ct);
				continue;
			}

			// Аватарку не тянем: на пачке каналов это лишние тяжёлые запросы,
			// её подтянет обновление информации о канале
			var destinationId = await storage.AddDestinationAsync(
				job.RepostSettingsId,
				info.Id,
				info.Title,
				info.Username,
				item.ParticipantsCount,
				chatType,
				item.DiscoveredChannelId,
				job.DefaultDelayMinSeconds,
				job.DefaultDelayMaxSeconds,
				job.DefaultRepostEveryNth,
				job.DefaultSkipProbability,
				job.DefaultMaxRepostsPerDay,
				ct);

			existingChatIds.Add(info.Id);
			await storage.UpdateItemAsync(item.ItemId, AddDestinationOutcome.Added, destinationId, null, ct);
		}

		if (pendingItems.Count > items.Count)
		{
			logger.LogInformation(
				"Задание {JobId}: обработано {Processed} каналов, остаток уходит в следующий проход",
				jobId, items.Count);
			await context.Publish(new ImportRepostDestinationsContract { JobId = jobId }, ct);

			return;
		}

		await storage.SetJobStatusAsync(jobId, RepostImportStatus.Completed, null, ct);
	}

	private async Task HandleRateLimitAsync(
		ImportJobData job,
		ImportJobPendingItem item,
		TelegramOperationResult<TelegramChatInfo> chatResult,
		CancellationToken ct
	)
	{
		var cooldownSeconds = chatResult.FloodWaitSeconds ?? options.DefaultCooldownSeconds;

		logger.LogWarning(
			"Telegram ограничил сессию {SessionId} на {Cooldown} сек. Задание {JobId} приостановлено",
			job.TelegramSessionId, cooldownSeconds, job.JobId);

		await storage.SetSessionFloodWaitAsync(
			job.TelegramSessionId,
			DateTimeOffset.UtcNow.AddSeconds(cooldownSeconds),
			ct);

		// В сыром тексте WTelegram секунды заменены на «X» (FLOOD_WAIT_X), поэтому
		// в журнал пишем понятную длительность, а не плейсхолдер
		await storage.UpdateItemAsync(item.ItemId, AddDestinationOutcome.RateLimited, null,
			$"ждём {cooldownSeconds} сек.", ct);

		// Остальные каналы остаются Pending — их подхватит ResumeRepostImportJobsWorker
		await storage.SetJobStatusAsync(job.JobId, RepostImportStatus.CooldownWait,
			$"Telegram ограничил сессию, ждём {cooldownSeconds} сек.", ct);
	}

	/// <summary>
	/// Останавливает задание: при лимите каналов каждый следующий канал упадёт так же,
	/// а лишние попытки вступления только приближают ограничение аккаунта
	/// </summary>
	/// <param name="job"></param>
	/// <param name="item"></param>
	/// <param name="error"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	private async Task StopOnChannelsLimitAsync(
		ImportJobData job,
		ImportJobPendingItem item,
		string? error,
		CancellationToken ct
	)
	{
		logger.LogWarning(
			"Сессия {SessionId} достигла лимита каналов. Задание {JobId} остановлено",
			job.TelegramSessionId, job.JobId);

		await storage.UpdateItemAsync(item.ItemId, AddDestinationOutcome.NotResolved, null, error, ct);
		await storage.MarkPendingItemsNotProcessedAsync(job.JobId, "аккаунт достиг лимита каналов Telegram", ct);
		await storage.SetJobStatusAsync(job.JobId, RepostImportStatus.Failed, error, ct);
	}

	private async Task DelayBeforeNextChannelAsync(CancellationToken ct)
	{
		var maxDelay = Math.Max(options.MinDelaySeconds, options.MaxDelaySeconds);
		var delaySeconds = Random.Shared.Next(options.MinDelaySeconds, maxDelay + 1);
		if (delaySeconds <= 0)
		{
			return;
		}

		logger.LogDebug("Пауза {Delay} сек. перед обработкой следующего канала", delaySeconds);
		await Task.Delay(TimeSpan.FromSeconds(delaySeconds), ct);
	}

	private static bool IsSessionRestricted(DateTimeOffset? floodWaitUntil) =>
		floodWaitUntil.HasValue && floodWaitUntil.Value > DateTimeOffset.UtcNow;

	private static (AddDestinationOutcome Outcome, string Error)? GetKnownRestriction(ImportJobPendingItem item)
	{
		if (item.CanSendMessages == false)
		{
			return (AddDestinationOutcome.NoWritePermission, "По данным последней проверки в канал нельзя писать");
		}

		if (item.CanSendMedia == false)
		{
			return (AddDestinationOutcome.NoMediaPermission,
				"По данным последней проверки в канал нельзя отправлять медиа");
		}

		return null;
	}

	private static AddDestinationOutcome? GetSkipOutcome(
		long chatId,
		long sourceChannelId,
		HashSet<long> existingChatIds
	)
	{
		if (chatId == sourceChannelId)
		{
			return AddDestinationOutcome.SourceChannel;
		}

		return existingChatIds.Contains(chatId)
			? AddDestinationOutcome.AlreadyAdded
			: null;
	}

	private static string? BuildIdentifier(ImportJobPendingItem item)
	{
		if (!string.IsNullOrWhiteSpace(item.Username))
		{
			return "@" + item.Username;
		}

		if (!string.IsNullOrWhiteSpace(item.InviteHash))
		{
			return "https://t.me/+" + item.InviteHash;
		}

		// Числовой id резолвится только по диалогам сессии — сработает,
		// если аккаунт уже состоит в канале
		return item.TelegramId?.ToString();
	}
}
