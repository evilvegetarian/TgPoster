using MediatR;

namespace TgPoster.API.Domain.UseCases.Accounts.RefreshToken;

public record RefreshTokenCommand(Guid RefreshToken) : IRequest<RefreshTokenResponse>;