using Microsoft.EntityFrameworkCore;
using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Storages;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public class SignOnStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
	private readonly PosterContext _context = fixture.GetDbContext();
	private readonly SignOnStorage sut = new(fixture.GetDbContext(), new GuidFactory());

	[Fact]
	public async Task CreateUserAsync_WithValidData_ShouldAddUserAndReturnId()
	{
		var username = "testUserss";
		var password = "password123";

		var userId = await sut.CreateUserAsync(username, password, CancellationToken.None);

		var userInDb = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);
		userInDb.ShouldNotBeNull();
		userInDb.UserName.Value.ShouldBe(username);
		userInDb.PasswordHash.ShouldBe(password);
	}

	[Fact]
	public async Task CreateUserAsync_WithDuplicateUsername_ShouldThrowDbUpdateException()
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
	public async Task CreateUserAsync_WithInvalidInputs_ShouldThrowDbUpdateException(string username, string? password)
	{
		await Should.ThrowAsync<DbUpdateException>(() =>
			sut.CreateUserAsync(username, password!, CancellationToken.None)
		);
	}

	[Theory]
	[InlineData(null, "password")]
	[InlineData("", "password")]
	[InlineData("n", "password")]
	[InlineData("   ", "password")]
	public async Task CreateUserAsync_WithInvalidInputs_ShouldThrowArgumentException(string? username, string password)
	{
		await Should.ThrowAsync<ArgumentException>(() =>
			sut.CreateUserAsync(username!, password, CancellationToken.None)
		);
	}

	[Fact]
	public async Task HaveUserNameAsync_WithExistingUsername_ShouldReturnTrue()
	{
		var username = "Mickle";
		new UserBuilder(_context)
			.WithName(username)
			.Create();

		var haveUser = await sut.HaveUserNameAsync(username, CancellationToken.None);
		haveUser.ShouldBeTrue();
	}

	[Fact]
	public async Task HaveUserNameAsync_WithNonExistingUsername_ShouldReturnFalse()
	{
		var haveUser = await sut.HaveUserNameAsync("FIlimon", CancellationToken.None);
		haveUser.ShouldBeFalse();
	}

	[Fact]
	public async Task HaveUserNameAsync_WithDelitedUser_ShouldReturnTrue()
	{
		var username = "dickle121";
		var user = new UserBuilder(_context)
			.WithName(username)
			.Create();

		_context.Users.Remove(user);
		await _context.SaveChangesAsync();

		var haveUser = await sut.HaveUserNameAsync(username, CancellationToken.None);
		haveUser.ShouldBeTrue();
	}

	[Fact]
	public async Task CreateUserAsync_WithSpecialCharactersInUsername_ShouldSucceed()
	{
		var username = "user!@#1";
		var password = "passwordssda";

		var id = await sut.CreateUserAsync(username, password, CancellationToken.None);

		var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == id);
		user.ShouldNotBeNull();
		user.UserName.Value.ShouldBe(username);
	}
}