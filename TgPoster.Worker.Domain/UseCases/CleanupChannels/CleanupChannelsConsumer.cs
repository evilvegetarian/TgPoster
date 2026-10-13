using MassTransit;
using Microsoft.Extensions.Logging;
using Shared.Enums;
using Shared.Telegram;
using TgPoster.Telegram.Abstractions;
using TgPoster.Telegram.Models;
using TgPoster.Worker.Domain.ConfigModels;

namespace TgPoster.Worker.Domain.UseCases.CleanupChannels;

/// <summary>
/// Выходит из неиспользуемых каналов и групп по одному с паузами, чтобы Telegram не ограничил аккаунт
/// </summary>
internal sealed class CleanupChannelsConsumer(
	ICleanupChannelsStorage storage,
	ITelegramMessageService messageService,
	ChannelCleanupOptions options,
	ILogger<CleanupChannelsConsumer> logger)
	: IConsumer<CleanupChannelsContract>
{
	public async Task Consume(ConsumeContext<CleanupChannelsContract> context)
	{
		var jobId = context.Message.JobId;
		var ct = context.CancellationToken;

		var job = await storage.GetJobAsync(jobId, ct);
		if (job is null)
		{
			logger.LogWarning("Задание на очистку каналов {JobId} не найдено", jobId);
			return;
		}

		if (job.Status is ChannelCleanupStatus.Completed or ChannelCleanupStatus.Failed)
		{
			return;
		}

		if (IsSessionRestricted(job.SessionFloodWaitUntil))
		{
			await storage.SetJobStatusAsync(jobId, ChannelCleanupStatus.CooldownWait,
				"Telegram ограничил сессию, обработка продолжится позже", ct);

			return;
		}

		var pendingItems = await storage.GetPendingItemsAsync(jobId, ct);
		if (pendingItems.Count == 0)
		{
			await storage.SetJobStatusAsync(jobId, ChannelCleanupStatus.Completed, null, ct);

			return;
		}

		await storage.SetJobStatusAsync(jobId, ChannelCleanupStatus.InProgress, null, ct);

		var items = pendingItems.Take(options.MaxItemsPerRun).ToList();

		// Задание могло ждать часами: за это время канал могли добавить в репост или парсинг
		var used = await storage.GetUsedChatsAsync(job.TelegramSessionId, ct);
		var needDelay = false;

		foreach (var item in items)
		{
			if (used.Contains(item.ChatId, item.Username))
			{
				await storage.UpdateItemAsync(item.ItemId, ChannelCleanupOutcome.InUse, null, ct);
				continue;
			}

			if (needDelay)
			{
				await DelayBeforeNextChannelAsync(ct);
			}

			needDelay = true;

			var peer = item.IsBasicGroup
				? TelegramPeer.Chat(item.ChatId)
				: TelegramPeer.Channel(item.ChatId, item.AccessHash);
			var result = await messageService.LeaveChatAsync(job.TelegramSessionId, peer, ct);

			if (result.IsSuccess)
			{
				var outcome = result.Value ? ChannelCleanupOutcome.Left : ChannelCleanupOutcome.NotMember;
				await storage.UpdateItemAsync(item.ItemId, outcome, null, ct);
				continue;
			}

			if (result.Status is TelegramOperationStatus.FloodWait or TelegramOperationStatus.SpamRestricted)
			{
				await PauseOnRateLimitAsync(job, result.FloodWaitSeconds, ct);

				return;
			}

			if (result.Status is TelegramOperationStatus.SessionNotFound)
			{
				await StopAsync(job, "сессия не авторизована или выключена", ct);

				return;
			}

			await storage.UpdateItemAsync(item.ItemId, ChannelCleanupOutcome.Failed, result.ErrorMessage, ct);
		}

		if (pendingItems.Count > items.Count)
		{
			await context.Publish(new CleanupChannelsContract { JobId = jobId }, ct);

			return;
		}

		await storage.SetJobStatusAsync(jobId, ChannelCleanupStatus.Completed, null, ct);
	}

	/// <summary>
	/// Ставит задание на паузу. Текущий канал остаётся в очереди и обработается после паузы
	/// </summary>
	/// <param name="job"></param>
	/// <param name="floodWaitSeconds"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	private async Task PauseOnRateLimitAsync(CleanupJobData job, int? floodWaitSeconds, CancellationToken ct)
	{
		var cooldownSeconds = floodWaitSeconds ?? options.DefaultCooldownSeconds;

		logger.LogWarning(
			"Telegram ограничил сессию {SessionId} на {Cooldown} сек. Очистка {JobId} приостановлена",
			job.TelegramSessionId, cooldownSeconds, job.JobId);

		await storage.SetSessionFloodWaitAsync(
			job.TelegramSessionId,
			DateTimeOffset.UtcNow.AddSeconds(cooldownSeconds),
			ct);

		// Остальные каналы остаются Pending — их подхватит ResumeChannelCleanupJobsWorker
		await storage.SetJobStatusAsync(job.JobId, ChannelCleanupStatus.CooldownWait,
			$"Telegram ограничил сессию, ждём {cooldownSeconds} сек.", ct);
	}

	private async Task StopAsync(CleanupJobData job, string reason, CancellationToken ct)
	{
		logger.LogWarning("Очистка {JobId} остановлена: {Reason}", job.JobId, reason);

		await storage.MarkPendingItemsNotProcessedAsync(job.JobId, reason, ct);
		await storage.SetJobStatusAsync(job.JobId, ChannelCleanupStatus.Failed, reason, ct);
	}

	private async Task DelayBeforeNextChannelAsync(CancellationToken ct)
	{
		var maxDelay = Math.Max(options.MinDelaySeconds, options.MaxDelaySeconds);
		var delaySeconds = Random.Shared.Next(options.MinDelaySeconds, maxDelay + 1);
		if (delaySeconds <= 0)
		{
			return;
		}

		await Task.Delay(TimeSpan.FromSeconds(delaySeconds), ct);
	}

	private static bool IsSessionRestricted(DateTimeOffset? floodWaitUntil) =>
		floodWaitUntil.HasValue && floodWaitUntil.Value > DateTimeOffset.UtcNow;
}
