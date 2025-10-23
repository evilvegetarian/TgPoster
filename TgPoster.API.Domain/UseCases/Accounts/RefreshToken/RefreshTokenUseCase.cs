using MediatR;
using Security.Interfaces;
using Security.Models;
using TgPoster.API.Domain.Exceptions;

namespace TgPoster.API.Domain.UseCases.Accounts.RefreshToken;

internal class RefreshTokenUseCase(IJwtProvider jwtProvider, IRefreshTokenStorage storage)
	: IRequestHandler<RefreshTokenCommand, RefreshTokenResponse>
{
	public async Task<RefreshTokenResponse> Handle(RefreshTokenCommand request, CancellationToken ct)
	{
		var userId = await storage.GetUserIdAsync(request.RefreshToken, ct);
		if (userId == Guid.Empty)
		{
			throw new UserNotFoundException();
		}

		var accessToken = jwtProvider.GenerateToken(new TokenServiceBuildTokenPayload(userId));
		var (refreshToken, refreshExpireTime) = jwtProvider.GenerateRefreshToken();

		await storage.UpdateRefreshSessionAsync(request.RefreshToken, refreshToken, refreshExpireTime, ct);

		return new RefreshTokenResponse
		{
			RefreshToken = refreshToken,
			RefreshTokenExpiration = refreshExpireTime,
			AccessToken = accessToken
		};
	}
}