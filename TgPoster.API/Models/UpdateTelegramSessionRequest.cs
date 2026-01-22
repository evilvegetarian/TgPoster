namespace TgPoster.API.Models;

public sealed record UpdateTelegramSessionRequest(string? Name, bool IsActive);