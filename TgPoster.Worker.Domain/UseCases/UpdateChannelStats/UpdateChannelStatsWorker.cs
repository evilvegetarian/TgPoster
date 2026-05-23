using Hangfire;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Enums;
using TgPoster.Telegram;
using TgPoster.Telegram.Abstractions;

namespace TgPoster.Worker.Domain.UseCases.UpdateChannelStats;

internal sealed class UpdateChannelStatsWorker(
	IUpdateChannelStatsStorage storage,
	ITelegramMessageService tgMessages,
	ILogger<UpdateChannelStatsWorker> logger,
	IHostApplicationLifetime lifetime)
{
	private const int BatchSize = 2;

	[DisableConcurrentExecution(3600)]
	public async Task UpdateStatsAsync()
	{
		var ct = lifetime.ApplicationStopping;

		var sessionId = await storage.GetSessionIdByPurposeAsync(TelegramSessionPurpose.UpdateStats, ct);
		if (sessionId is null)
		{
			logger.LogWarning("Нет активной авторизованной сессии с назначением UpdateStats");
			return;
		}

		var channels = await storage.GetChannelsToUpdateAsync(BatchSize, ct);
		if (channels.Count == 0)
		{
			logger.LogInformation("Нет каналов для обновления статистики подписчиков");
			return;
		}

		logger.LogInformation("Начинаем обновление статистики для {Count} каналов", channels.Count);

		var updated = 0;

		foreach (var channel in channels)
		{
			ct.ThrowIfCancellationRequested();

			var resolved = await tgMessages.ResolveChannelAsync(sessionId.Value, channel.Username, ct);
			if (!resolved.IsSuccess)
			{
				logger.LogDebug("Не удалось разрешить @{Username} ({Status}), пропускаем", channel.Username,
					resolved.Status);
				await Task.Delay(TimeSpan.FromSeconds(3), ct);
				continue;
			}

			var fullResult = await tgMessages.GetFullChannelAsync(
				sessionId.Value,
				resolved.Value!.Peer,
				ct);

			if (fullResult.IsSuccess && fullResult.Value.HasValue)
			{
				await storage.UpdateParticipantsCountAsync(channel.Id, fullResult.Value.Value, ct);
				updated++;
			}
			else
			{
				logger.LogDebug("Не удалось получить статистику @{Username} ({Status})", channel.Username,
					fullResult.Status);
			}

			await Task.Delay(TimeSpan.FromSeconds(3), ct);
		}

		logger.LogInformation("Обновлена статистика для {Updated}/{Total} каналов", updated, channels.Count);
	}
}
