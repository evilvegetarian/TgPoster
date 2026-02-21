using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Language.Flow;
using Security.Authentication;
using Shouldly;
using TgPoster.API.Domain.Exceptions;
using TgPoster.API.Domain.UseCases.Accounts.RefreshToken;

namespace TgPoster.API.Domain.Tests.Account;

public class RefreshTokenUseCaseShould
{
	private readonly Mock<IRefreshTokenStorage> storage;
	private readonly Mock<IJwtProvider> jwt;
	private readonly ISetup<IRefreshTokenStorage, Task<Guid>> getUserIdSetup;
	private readonly ISetup<IRefreshTokenStorage, Task<Guid>> getPreviousTokenSetup;
	private readonly ISetup<IJwtProvider, string> generateAccessTokenSetup;
	private readonly ISetup<IJwtProvider, (Guid, DateTimeOffset)> generateRefreshTokenSetup;
	private readonly RefreshTokenUseCase sut;

	public RefreshTokenUseCaseShould()
	{
		storage = new Mock<IRefreshTokenStorage>();
		jwt = new Mock<IJwtProvider>();

		getUserIdSetup = storage.Setup(s =>
			s.GetUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()));

		getPreviousTokenSetup = storage.Setup(s =>
			s.GetUserIdByPreviousTokenAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()));

		generateAccessTokenSetup = jwt.Setup(j =>
			j.GenerateToken(It.IsAny<TokenServiceBuildTokenPayload>()));

		generateRefreshTokenSetup = jwt.Setup(j => j.GenerateRefreshToken());

		sut = new RefreshTokenUseCase(jwt.Object, storage.Object, NullLogger<RefreshTokenUseCase>.Instance);
	}

	[Fact]
	public async Task ReturnNewTokens_WhenRefreshTokenIsValid()
	{
		var userId = Guid.NewGuid();
		var oldRefreshToken = Guid.NewGuid();
		var newRefreshToken = Guid.NewGuid();
		var newExpiration = DateTimeOffset.UtcNow.AddDays(7);

		getUserIdSetup.ReturnsAsync(userId);
		generateAccessTokenSetup.Returns("new_access_token");
		generateRefreshTokenSetup.Returns((newRefreshToken, newExpiration));

		var result = await sut.Handle(new RefreshTokenCommand(oldRefreshToken), CancellationToken.None);

		result.AccessToken.ShouldBe("new_access_token");
		result.RefreshToken.ShouldBe(newRefreshToken);
		result.RefreshTokenExpiration.ShouldBe(newExpiration);
	}

	[Fact]
	public async Task UpdateRefreshSession_WhenTokenIsValid()
	{
		var userId = Guid.NewGuid();
		var oldRefreshToken = Guid.NewGuid();
		var newRefreshToken = Guid.NewGuid();
		var newExpiration = DateTimeOffset.UtcNow.AddDays(7);

		getUserIdSetup.ReturnsAsync(userId);
		generateAccessTokenSetup.Returns("token");
		generateRefreshTokenSetup.Returns((newRefreshToken, newExpiration));

		await sut.Handle(new RefreshTokenCommand(oldRefreshToken), CancellationToken.None);

		storage.Verify(s =>
			s.UpdateRefreshSessionAsync(oldRefreshToken, newRefreshToken, newExpiration, It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task ThrowUserNotFoundException_WhenTokenInvalidAndNoPreviousToken()
	{
		var invalidToken = Guid.NewGuid();
		getUserIdSetup.ReturnsAsync(Guid.Empty);
		getPreviousTokenSetup.ReturnsAsync(Guid.Empty);

		await Should.ThrowAsync<UserNotFoundException>(
			async () => await sut.Handle(new RefreshTokenCommand(invalidToken), CancellationToken.None));
	}

	[Fact]
	public async Task RevokeAllSessions_WhenTokenReplayDetected()
	{
		var replayedToken = Guid.NewGuid();
		var userId = Guid.NewGuid();
		getUserIdSetup.ReturnsAsync(Guid.Empty);
		getPreviousTokenSetup.ReturnsAsync(userId);

		await Should.ThrowAsync<UserNotFoundException>(
			async () => await sut.Handle(new RefreshTokenCommand(replayedToken), CancellationToken.None));

		storage.Verify(s =>
			s.RevokeAllUserSessionsAsync(userId, It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task NotRevokeAnySessions_WhenTokenIsValid()
	{
		var userId = Guid.NewGuid();
		getUserIdSetup.ReturnsAsync(userId);
		generateAccessTokenSetup.Returns("token");
		generateRefreshTokenSetup.Returns((Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(7)));

		await sut.Handle(new RefreshTokenCommand(Guid.NewGuid()), CancellationToken.None);

		storage.Verify(s =>
			s.RevokeAllUserSessionsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
			Times.Never);
	}
}
