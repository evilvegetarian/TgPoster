using MediatR;

namespace TgPoster.API.Domain.UseCases.TelegramSessions.SendPassword;

public sealed record SendPasswordCommand(Guid SessionId, string Password) : IRequest<SendPasswordResponse>;