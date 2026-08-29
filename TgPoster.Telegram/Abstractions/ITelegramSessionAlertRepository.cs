using TgPoster.Telegram.Models;

namespace TgPoster.Telegram.Abstractions;

/// <summary>
///     Репозиторий для получения бота, через которого отправляются оповещения о проблемах с сессией
/// </summary>
public interface ITelegramSessionAlertRepository
{
	/// <summary>
	///     Получает данные бота-оповещателя, привязанного к сессии
	/// </summary>
	/// <param name="sessionId">Id Telegram сессии</param>
	/// <param name="ct">Токен отмены</param>
	/// <returns>Данные для отправки оповещения или null, если бот к сессии не привязан</returns>
	Task<TelegramSessionAlertTarget?> GetAlertTargetAsync(Guid sessionId, CancellationToken ct);
}
