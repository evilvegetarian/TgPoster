using Microsoft.EntityFrameworkCore;
using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Storages;

namespace TgPoster.Storage.Tests.Tests;

public class RefreshTokenStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
    private readonly PosterContext context = fixture.GetDbContext();
    private readonly Helper helper = new(fixture.GetDbContext());
    private readonly RefreshTokenStorage sut = new(fixture.GetDbContext());

    [Fact]
    public async Task GetUserIdAsync_WithValidRefreshToken_ShouldReturnUserId()
    {
        var refreshSession = await helper.CreateRefreshSessionAsync();

        var result = await sut.GetUserIdAsync(refreshSession.RefreshToken, CancellationToken.None);

        result.ShouldBe(refreshSession.UserId);
    }

    [Fact]
    public async Task GetUserIdAsync_WithExpiredRefreshToken_ShouldReturnEmptyGuid()
    {
        var user = await helper.CreateUserAsync();
        var expiredRefreshSession = new TgPoster.Storage.Data.Entities.RefreshSession
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
        var refreshSession = await helper.CreateRefreshSessionAsync();
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

        var exception = await Should.ThrowAsync<NullReferenceException>(async () =>
            await sut.UpdateRefreshSessionAsync(
                nonExistingToken, 
                newRefreshToken, 
                newExpiresAt, 
                CancellationToken.None));

        exception.ShouldNotBeNull();
    }
}
