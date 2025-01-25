using Microsoft.EntityFrameworkCore;
using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.VO;
using TgPoster.Storage.Storages;

namespace TgPoster.Storage.Tests;

public class SignInStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
    private readonly SignInStorage sut = new(fixture.GetDbContext());
    private readonly PosterContext context = fixture.GetDbContext();
    Helper helper = new(fixture.GetDbContext());

    [Fact]
    public async Task GetUserAsync_ShouldReturnUser()
    {
        var newUser = new User
        {
            Id = Guid.Parse("33f99d4e-e822-41c6-8858-6df6b21544ff"),
            UserName = new UserName("Kirilll"),
            PasswordHash = "afs3r2fefewfwe"
        };

        await context.Users.AddAsync(newUser);
        await context.SaveChangesAsync();
        var user = await sut.GetUserAsync(newUser.UserName.Value);
        user.ShouldNotBeNull();
        user.Id.ShouldBe(newUser.Id);
    }

    [Fact]
    public async Task GetUserAsync_NotExist_ShouldNotReturnUser()
    {
        var user = await sut.GetUserAsync("not-exist");
        user.ShouldBeNull();
    }

    [Fact]
    public async Task CreateRefreshSessionAsync_ShouldExistRefreshSession()
    {
        var user = await helper.CreateUserAsync();
        var refreshToken = Guid.NewGuid();
        await sut.CreateRefreshSession(user.Id, refreshToken, DateTimeOffset.UtcNow.AddDays(5));

        var refreshSession =
            await context.RefreshSessions.Where(x => x.RefreshToken == refreshToken).FirstOrDefaultAsync();
        refreshSession.ShouldNotBeNull();
    }
}