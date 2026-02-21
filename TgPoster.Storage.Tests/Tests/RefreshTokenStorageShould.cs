using Microsoft.EntityFrameworkCore;
using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Storages;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public class RefreshTokenStorageShould : IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context;
	private readonly RefreshTokenStorage sut;

	public RefreshTokenStorageShould(StorageTestFixture fixture)
	{
		context = fixture.GetDbContext();
		sut = new(context);
	}

	[Fact]
	public async Task GetUserIdAsync_WithValidRefreshToken_ShouldReturnUserId()
	{
		var refreshSession = await new RefreshSessionBuilder(context).CreateAsync();

		var result = await sut.GetUserIdAsync(refreshSession.RefreshToken, CancellationToken.None);

		result.ShouldBe(refreshSession.UserId);
	}

	[Fact]
	public async Task GetUserIdAsync_WithExpiredRefreshToken_ShouldReturnEmptyGuid()
	{
		var user = await new UserBuilder(context).CreateAsync();
		var expiredRefreshSession = new RefreshSession
		{
			Id = Guid.NewGuid(),
			UserId = user.Id,
			RefreshToken = Guid.NewGuid(),
			ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1) // Expired
		};
		await context.RefreshSessions.AddAsync(expiredRefreshSession);
		await context.SaveChangesAsync();

		var result = await sut.GetUserIdAsync(expiredRefreshSession.RefreshToken, CancellationToken.None);

		result.ShouldBe(Guid.Empty);
	}

	[Fact]
	public async Task GetUserIdAsync_WithNonExistingRefreshToken_ShouldReturnEmptyGuid()
	{
		var nonExistingToken = Guid.NewGuid();

		var result = await sut.GetUserIdAsync(nonExistingToken, CancellationToken.None);

		result.ShouldBe(Guid.Empty);
	}

	[Fact]
	public async Task UpdateRefreshSessionAsync_WithValidData_ShouldUpdateRefreshSession()
	{
		var refreshSession = await new RefreshSessionBuilder(context).CreateAsync();
		var newRefreshToken = Guid.NewGuid();
		var newExpiresAt = DateTimeOffset.UtcNow.AddDays(14);

		await sut.UpdateRefreshSessionAsync(
			refreshSession.RefreshToken,
			newRefreshToken,
			newExpiresAt,
			CancellationToken.None);

		var updatedSession = await context.RefreshSessions
			.FirstOrDefaultAsync(x => x.Id == refreshSession.Id);

		updatedSession.ShouldNotBeNull();
		updatedSession.RefreshToken.ShouldBe(newRefreshToken);
	}

	[Fact]
	public async Task UpdateRefreshSessionAsync_WithNonExistingToken_ShouldNotThrow()
	{
		var nonExistingToken = Guid.NewGuid();
		var newRefreshToken = Guid.NewGuid();
		var newExpiresAt = DateTimeOffset.UtcNow.AddDays(14);

		await sut.UpdateRefreshSessionAsync(
			nonExistingToken,
			newRefreshToken,
			newExpiresAt,
			CancellationToken.None);
	}
}