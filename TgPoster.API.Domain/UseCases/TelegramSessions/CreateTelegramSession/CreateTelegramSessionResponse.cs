namespace TgPoster.API.Domain.UseCases.TelegramSessions.CreateTelegramSession;

public sealed record CreateTelegramSessionResponse(Guid Id, string Name, bool IsActive, string AuthStatus);