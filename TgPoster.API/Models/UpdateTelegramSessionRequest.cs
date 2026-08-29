namespace TgPoster.API.Models;

/// <summary>
///     Запрос на обновление Telegram сессии
/// </summary>
/// <param name="Name">Новое название сессии (опционально)</param>
/// <param name="IsActive">Активна ли сессия</param>
/// <param name="ProxyId">ID прокси (null = без прокси)</param>
/// <param name="NotificationBotId">ID бота для оповещений о проблемах с аккаунтом (null = без оповещений)</param>
public sealed record UpdateTelegramSessionRequest(
	string? Name,
	bool IsActive,
	Guid? ProxyId,
	Guid? NotificationBotId
);
