using MediatR;

namespace TgPoster.API.Domain.UseCases.TelegramSessions.DeleteTelegramSession;

public sealed record DeleteTelegramSessionCommand(Guid SessionId) : IRequest;