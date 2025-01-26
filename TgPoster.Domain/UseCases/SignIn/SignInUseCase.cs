using Auth;
using MediatR;
using Microsoft.AspNetCore.Http;
using TgPoster.Domain.Exceptions;

namespace TgPoster.Domain.UseCases.SignIn;

internal sealed class SignInUseCase(
    ISignInStorage storage,
    IPasswordHasher passwordHasher,
    IJwtProvider jwtProvider,
    IHttpContextAccessor httpContext
) : IRequestHandler<SignInCommand, SignInResponse>
{
    public async Task<SignInResponse> Handle(SignInCommand request, CancellationToken cancellationToken)
    {
        var user = await storage.GetUserAsync(request.Login, cancellationToken);
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

        //Чтобы проще жилось
        jwtProvider.AddTokenToCookie(httpContext.HttpContext!, accessToken);
        await storage.CreateRefreshSession(user.Id, refreshToken, refreshExpireTime, cancellationToken);

        return new SignInResponse
        {
            RefreshToken = refreshToken,
            RefreshTokenExpiration = refreshExpireTime,
            AccessToken = accessToken,
        };
    }
}