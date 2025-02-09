using Microsoft.AspNetCore.Http;
using Moq;
using Moq.Language.Flow;
using Security.Interfaces;
using Security.Models;
using Shouldly;
using TgPoster.Domain.Exceptions;
using TgPoster.Domain.Models;
using TgPoster.Domain.UseCases.Accounts.SignIn;

namespace TgPoster.Domain.Tests.SignIn;

public class SignInUseCaseShould
{
    private readonly ISetup<IPasswordHasher, bool> checkPasswordSetup;
    private readonly ISetup<IJwtProvider, string> generateAccessTokenSetup;
    private readonly ISetup<IJwtProvider, (Guid, DateTimeOffset)> generateRefreshTokenSetup;
    private readonly ISetup<ISignInStorage, Task<UserDto?>> getUserSetup;
    private readonly Mock<IJwtProvider> jwt;
    private readonly Mock<ISignInStorage> storage;
    private readonly SignInUseCase sut;

    public SignInUseCaseShould()
    {
        storage = new Mock<ISignInStorage>();
        jwt = new Mock<IJwtProvider>();
        var hasher = new Mock<IPasswordHasher>();
        var context = new Mock<IHttpContextAccessor>();
        getUserSetup = storage.Setup(s => s.GetUserAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()));
        checkPasswordSetup = hasher.Setup(x => x.CheckPassword(It.IsAny<string>(), It.IsAny<string>()));
        generateAccessTokenSetup = jwt.Setup(s => s.GenerateToken(It.IsAny<TokenServiceBuildTokenPayload>()));
        generateRefreshTokenSetup = jwt.Setup(s => s.GenerateRefreshToken());

        sut = new SignInUseCase(storage.Object, hasher.Object, jwt.Object, context.Object);
    }

    [Fact]
    public async Task ThrowException_IfUserNotFound()
    {
        var command = new SignInCommand("login", "password");
        getUserSetup.ReturnsAsync(null as UserDto);
        await Should.ThrowAsync<UserNotFoundException>(async () =>
            await sut.Handle(command, It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task ThrowException_IfPasswordIsNotCorrect()
    {
        var command = new SignInCommand("login", "password");
        var user = new UserDto
        {
            Id = Guid.Parse("b880c95a-20eb-47db-83d3-b97a43db867d"),
            Username = "login",
            PasswordHash = "passHash"
        };
        getUserSetup.ReturnsAsync(user);
        checkPasswordSetup.Returns(false);
        await Should.ThrowAsync<InvalidPasswordException>(async () =>
            await sut.Handle(command, It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task ValidCredentials_ReturnsSignInResponse()
    {
        var command = new SignInCommand("valid_user", "validpassword");
        var user = new UserDto { Id = Guid.NewGuid(), Username = "valid_user", PasswordHash = "hashed_valid_password" };
        var accessToken = "access_token";
        var refreshToken = Guid.Parse("b0f737e2-1a05-49d2-b4b0-b659a8eddeed");
        var refreshExpireTime = DateTimeOffset.UtcNow.AddDays(7);

        getUserSetup.ReturnsAsync(user);
        checkPasswordSetup.Returns(true);
        generateAccessTokenSetup.Returns(accessToken);
        generateRefreshTokenSetup.Returns((refreshToken, refreshExpireTime));

        var response = await sut.Handle(command, It.IsAny<CancellationToken>());
        response.ShouldNotBeNull();
        response.RefreshToken.ShouldBe(refreshToken);
        response.AccessToken.ShouldBe(accessToken);
        response.RefreshTokenExpiration.ShouldBe(refreshExpireTime);

        jwt.Verify(j => j.AddTokenToCookie(It.IsAny<HttpContext>(), accessToken), Times.Once);

        storage.Verify(
            s => s.CreateRefreshSession(user.Id, refreshToken, refreshExpireTime, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}