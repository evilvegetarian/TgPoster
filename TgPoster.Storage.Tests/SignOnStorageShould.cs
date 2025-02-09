using Microsoft.EntityFrameworkCore;
using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.VO;
using TgPoster.Storage.Storages;

namespace TgPoster.Storage.Tests;

public class SignOnStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
    private readonly PosterContext _context = fixture.GetDbContext();
    private readonly SignOnStorage sut = new(fixture.GetDbContext(), new GuidFactory());

    [Fact]
    public async Task CreateUserAsync_ShouldAddUserAndReturnId()
    {
        var username = "testuser";
        var password = "password123";

        var result = await sut.CreateUserAsync(username, password, CancellationToken.None);

        var userInDb = await _context.Users.FindAsync(result);
        userInDb.ShouldNotBeNull();
        userInDb.UserName.Value.ShouldBe(username);
        userInDb.PasswordHash.ShouldBe(password);
    }

    [Fact]
    public async Task CreateUserAsync_WithDuplicateUsername_ShouldThrowInvalidOperationException()
    {
        var username = "duplicateUser";
        var password1 = "password1";
        var password2 = "password2";

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

    [Fact]
    public async Task HaveUserNameAsync_ShouldReturnTrueIfUserExists()
    {
        var user = new User
        {
            Id = Guid.Parse("55d75f74-5c1b-43a3-8cae-777e80b68aaf"),
            UserName = new UserName("Mickle"),
            PasswordHash = "password123"
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
        var haveUser = await sut.HaveUserNameAsync("Mickle");
        haveUser.ShouldBeTrue();
    }

    [Fact]
    public async Task HaveUserNameAsync_ShouldReturnFalseIfUserNotExists()
    {
        var haveUser = await sut.HaveUserNameAsync("FIlimon");
        haveUser.ShouldBeFalse();
    }
}