using TgPoster.Telegram.Models;

namespace TgPoster.Telegram.Abstractions;

/// <summary>
///     Репозиторий для получения активного HTTP-прокси из БД для исходящих запросов к t.me.
/// </summary>
public interface ITelegramHttpProxyRepository
{
	/// <summary>
	///     Получить первый активный HTTP-прокси для исходящих запросов к t.me
	/// </summary>
	/// <param name="ct">Токен отмены.</param>
	/// <returns>Прокси или <c>null</c>, если активных HTTP-прокси нет.</returns>
	Task<ProxyDto?> GetActiveHttpProxyAsync(CancellationToken ct);
}