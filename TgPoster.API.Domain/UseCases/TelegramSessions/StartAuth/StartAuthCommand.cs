using MediatR;

namespace TgPoster.API.Domain.UseCases.TelegramSessions.StartAuth;

public sealed record StartAuthCommand(Guid SessionId) : IRequest<StartAuthResponse>;