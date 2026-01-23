using Microsoft.EntityFrameworkCore;
using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Storages;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public sealed class DeleteTelegramSessionStorageShould(StorageTestFixture fixture)
	: IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context = fixture.GetDbContext();
	private readonly DeleteTelegramSessionStorage sut = new(fixture.GetDbContext());

	[Fact]
	public async Task ExistsAsync_WithValidUserAndSession_ShouldReturnTrue()
	{
		var user = await new UserBuilder(context).CreateAsync();
		var session = await new TelegramSessionBuilder(context)
			.WithUserId(user.Id)
			.CreateAsync();

		var result = await sut.ExistsAsync(user.Id, session.Id, CancellationToken.None);

		result.ShouldBeTrue();
	}

	[Fact]
	public async Task ExistsAsync_WithWrongUserId_ShouldReturnFalse()
	{
		var user = await new UserBuilder(context).CreateAsync();
		var anotherUser = await new UserBuilder(context).CreateAsync();
		var session = await new TelegramSessionBuilder(context)
			.WithUserId(user.Id)
			.CreateAsync();

		var result = await sut.ExistsAsync(anotherUser.Id, session.Id, CancellationToken.None);

		result.ShouldBeFalse();
	}

	[Fact]
	public async Task ExistsAsync_WithNonExistentSession_ShouldReturnFalse()
	{
		var user = await new UserBuilder(context).CreateAsync();

		var result = await sut.ExistsAsync(user.Id, Guid.NewGuid(), CancellationToken.None);

		result.ShouldBeFalse();
	}

	[Fact]
	public async Task DeleteAsync_WithValidSession_ShouldDeleteSession()
	{
		var user = await new UserBuilder(context).CreateAsync();
		var session = await new TelegramSessionBuilder(context)
			.WithUserId(user.Id)
			.CreateAsync();

		await sut.DeleteAsync(session.Id, CancellationToken.None);

		var deleted = await context.TelegramSessions
			.FirstOrDefaultAsync(s => s.Id == session.Id, CancellationToken.None);

		deleted.ShouldBeNull();
	}

	[Fact]
	public async Task DeleteAsync_WithNonExistentSession_ShouldThrow()
	{
		await Should.ThrowAsync<InvalidOperationException>(async () =>
		{
			await sut.DeleteAsync(Guid.NewGuid(), CancellationToken.None);
		});
	}
}
