using Microsoft.EntityFrameworkCore;
using Npgsql;
using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.VO;
using TgPoster.Storage.Storages;

namespace TgPoster.Storage.Tests;

public class SignOnStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
    private readonly SignOnStorage sut = new(fixture.GetDbContext(), new GuidFactory());
    private readonly PosterContext _context = fixture.GetDbContext();

    [Fact]
    public async Task CreateUserAsync_ShouldAddUserAndReturnId()
    {
        string username = "testuser";
        string password = "password123";

        var result = await sut.CreateUserAsync(username, password, CancellationToken.None);

        var userInDb = await _context.Users.FindAsync(result);
        userInDb.ShouldNotBeNull();
        userInDb.UserName.Value.ShouldBe(username);
        userInDb.PasswordHash.ShouldBe(password);
    }

    [Fact]
    public async Task CreateUserAsync_WithDuplicateUsername_ShouldThrowInvalidOperationException()
    {
        string username = "duplicateUser";
        string password1 = "password1";
        string password2 = "password2";

        var firstUserId = await sut.CreateUserAsync(username, password1, CancellationToken.None);

        await Should.ThrowAsync<DbUpdateException>(() =>
            sut.CreateUserAsync(username, password2, CancellationToken.None)
        );

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == firstUserId);
        user.ShouldNotBeNull();
        user.UserName.Value.ShouldBe(username);
    }

    [Theory]
    [InlineData("NewUser", null)]
    public async Task CreateUserAsync_WithInvalidInputs_ShouldThrowDbUpdateException(string username, string password)
    {
        await Should.ThrowAsync<DbUpdateException>(() =>
            sut.CreateUserAsync(username, password, CancellationToken.None)
        );
    }

    [Theory]
    [InlineData(null, "password")]
    [InlineData("", "password")]
    [InlineData("n", "password")]
    [InlineData("   ", "password")]
    public async Task CreateUserAsync_WithInvalidInputs_ShouldThrowArgumentException(string username, string password)
    {
        await Should.ThrowAsync<ArgumentException>(() =>
            sut.CreateUserAsync(username, password, CancellationToken.None)
        );
    }
}