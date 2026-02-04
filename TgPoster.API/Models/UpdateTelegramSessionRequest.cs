namespace TgPoster.API.Models;

/// <summary>
/// Запрос на обновление Telegram сессии
/// </summary>
/// <param name="Name">Новое название сессии (опционально)</param>
/// <param name="IsActive">Активна ли сессия</param>
public sealed record UpdateTelegramSessionRequest(string? Name, bool IsActive);