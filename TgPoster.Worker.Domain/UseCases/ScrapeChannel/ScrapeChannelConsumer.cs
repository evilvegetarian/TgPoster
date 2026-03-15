using MassTransit;
using Microsoft.Extensions.Logging;
using Shared.Telegram;
using Shared.TgStat;

namespace TgPoster.Worker.Domain.UseCases.ScrapeChannel;

internal sealed class ScrapeChannelConsumer(
	ITgStatScrapingService scrapingService,
	IBus bus,
	IScrapeChannelStorage storage,
	ILogger<ScrapeChannelConsumer> logger) : IConsumer<ScrapeChannelContract>
{
	public async Task Consume(ConsumeContext<ScrapeChannelContract> context)
	{
		var url = context.Message.Url;
		var ct = context.CancellationToken;
		await Task.Delay(TimeSpan.FromMinutes(1), ct);
		logger.LogInformation("Скрейпим канал с TGStat: {Url}", url);

		var detail = await scrapingService.ScrapeChannelDetailAsync(url, ct);
		if (detail is null)
		{
			await Task.Delay(TimeSpan.FromMinutes(10), ct);
			await bus.Publish(context.Message, ct);
			logger.LogWarning("Не удалось спарсить канал: {Url}", url);
			return;
		}

		if (string.IsNullOrEmpty(detail.Username))
		{
			logger.LogWarning("Канал без username, пропускаем: {Url}", url);
			return;
		}

		await storage.UpsertChannelAsync(
			detail.Username,
			detail.Title,
			detail.Description,
			detail.AvatarUrl,
			detail.ParticipantsCount,
			detail.PeerType,
			detail.TgUrl,
			ct);

		logger.LogInformation("Канал сохранён: {Title} (@{Username})", detail.Title, detail.Username);
	}
}