using Microsoft.Extensions.Logging;
using Shared.Telegram;
using Telegram.Bot;

namespace TgPoster.Worker.Domain.UseCases.CrossPosting.Media;

/// <summary>
///     Реализация загрузки файлов из Telegram Bot API
/// </summary>
internal sealed class TelegramFileDownloader(TelegramBotManager botManager, ILogger<TelegramFileDownloader> logger)
	: ITelegramFileDownloader
{
	public async Task<byte[]?> DownloadAsync(string botToken, string tgFileId, CancellationToken ct)
	{
		try
		{
			var bot = botManager.GetClient(botToken);
			await using var stream = new MemoryStream();
			await bot.GetInfoAndDownloadFile(tgFileId, stream, ct);
			return stream.ToArray();
		}
		catch (OperationCanceledException) when (ct.IsCancellationRequested)
		{
			throw;
		}
		catch (Exception ex)
		{
			logger.LogWarning(ex, "Не удалось скачать файл {TgFileId} для кросс-поста", tgFileId);
			return null;
		}
	}
}
