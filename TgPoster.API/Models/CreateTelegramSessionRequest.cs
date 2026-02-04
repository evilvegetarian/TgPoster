namespace TgPoster.API.Models;

/// <summary>
/// Запрос на создание Telegram сессии
/// </summary>
/// <param name="ApiId">API ID приложения Telegram</param>
/// <param name="ApiHash">API Hash приложения Telegram</param>
/// <param name="PhoneNumber">Номер телефона</param>
/// <param name="Name">Название сессии (опционально)</param>
public sealed record CreateTelegramSessionRequest(
	string ApiId,
	string ApiHash,
	string PhoneNumber,
	string? Name
);