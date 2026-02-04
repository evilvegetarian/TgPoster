namespace TgPoster.API.Models;

/// <summary>
/// Запрос на отправку пароля
/// </summary>
/// <param name="Password">Пароль</param>
public sealed record SendPasswordRequest(string Password);