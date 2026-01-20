using MediatR;

namespace TgPoster.API.Domain.UseCases.TelegramSessions.VerifyCode;

public sealed record VerifyCodeCommand(Guid SessionId, string Code) : IRequest<VerifyCodeResponse>;
