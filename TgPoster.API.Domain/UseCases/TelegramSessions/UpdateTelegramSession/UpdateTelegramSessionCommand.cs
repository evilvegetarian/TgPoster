using MediatR;

namespace TgPoster.API.Domain.UseCases.TelegramSessions.UpdateTelegramSession;

public sealed record UpdateTelegramSessionCommand(
	Guid SessionId,
	string? Name,
	bool IsActive
) : IRequest;