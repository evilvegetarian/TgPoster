using Shared.TgStat.Models;

namespace Shared.TgStat;

/// <summary>
///     Сервис скрейпинга каналов с tgstat.ru.
/// </summary>
public interface ITgStatScrapingService
{
	/// <summary>
	///     Парсит детальную информацию о канале по URL.
	/// </summary>
	/// <param name="channelUrl">URL канала на tgstat.</param>
	/// <param name="ct">Токен отмены.</param>
	Task<TgStatChannelDetailDto?> ScrapeChannelDetailAsync(string channelUrl, CancellationToken ct);
}
