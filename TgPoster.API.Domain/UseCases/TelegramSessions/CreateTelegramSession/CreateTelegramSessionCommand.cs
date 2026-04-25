using MediatR;

namespace TgPoster.API.Domain.UseCases.TelegramSessions.CreateTelegramSession;

public sealed record CreateTelegramSessionCommand(
	string ApiId,
	string ApiHash,
	string PhoneNumber,
	string? Name,
	Guid? ProxyId
) : IRequest<CreateTelegramSessionResponse>;