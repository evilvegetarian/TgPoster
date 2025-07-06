using MediatR;
using Microsoft.AspNetCore.Http;
using Security.Interfaces;
using Security.Models;
using TgPoster.API.Domain.Exceptions;

namespace TgPoster.API.Domain.UseCases.Accounts.SignIn;

internal sealed class SignInUseCase(
    ISignInStorage storage,
    IPasswordHasher passwordHasher,
    IJwtProvider jwtProvider,
    IHttpContextAccessor httpContext
) : IRequestHandler<SignInCommand, SignInResponse>
{
    public async Task<SignInResponse> Handle(SignInCommand request, CancellationToken ct)
    {
        var user = await storage.GetUserAsync(request.Login, ct);
        if (user == null)
        {
            throw new UserNotFoundException();
        }

        var validPassword = passwordHasher.CheckPassword(request.Password, user.PasswordHash);
        if (!validPassword)
        {
            throw new InvalidPasswordException();
        }

        var accessToken = jwtProvider.GenerateToken(new TokenServiceBuildTokenPayload(user.Id));
        var (refreshToken, refreshExpireTime) = jwtProvider.GenerateRefreshToken();

        //TODO: Чтобы проще жилось, переделать в будущем
        jwtProvider.AddTokenToCookie(httpContext.HttpContext!, accessToken);
        await storage.CreateRefreshSessionAsync(user.Id, refreshToken, refreshExpireTime, ct);
        return new SignInResponse
        {
            RefreshToken = refreshToken,
            RefreshTokenExpiration = refreshExpireTime,
            AccessToken = accessToken
        };
    }
}