using TgPoster.Telegram.Models;

namespace TgPoster.Telegram.Abstractions;

/// <summary>
///     Сервис оповещений о проблемах с Telegram аккаунтом.
///     Пишет владельцу в чат через бота, привязанного к сессии
/// </summary>
public interface ITelegramSessionAlertService
{
	/// <summary>
	///     Отправляет оповещение о проблеме с аккаунтом. Никогда не бросает исключений:
	///     сбой оповещения не должен ломать основной сценарий
	/// </summary>
	/// <param name="sessionId">Id Telegram сессии, с которой возникла проблема</param>
	/// <param name="problem">Тип проблемы</param>
	/// <param name="details">Технические подробности (текст ошибки Telegram)</param>
	/// <param name="ct">Токен отмены</param>
	Task NotifyAsync(
		Guid sessionId,
		TelegramSessionProblem problem,
		string? details = null,
		CancellationToken ct = default
	);
}
