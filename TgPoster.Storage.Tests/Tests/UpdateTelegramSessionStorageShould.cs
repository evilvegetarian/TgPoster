using Microsoft.EntityFrameworkCore;
using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Storages;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public sealed class UpdateTelegramSessionStorageShould(StorageTestFixture fixture)
	: IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context = fixture.GetDbContext();
	private readonly UpdateTelegramSessionStorage sut = new(fixture.GetDbContext());

	[Fact]
	public async Task GetByIdAsync_WithValidUserAndSession_ShouldReturnSession()
	{
		var user = await new UserBuilder(context).CreateAsync();
		var session = await new TelegramSessionBuilder(context)
			.WithUserId(user.Id)
			.CreateAsync();

		var result = await sut.GetByIdAsync(user.Id, session.Id, CancellationToken.None);

		result.ShouldNotBeNull();
		result.Id.ShouldBe(session.Id);
		result.Name.ShouldBe(session.Name);
		result.IsActive.ShouldBe(session.IsActive);
	}

	[Fact]
	public async Task GetByIdAsync_WithWrongUserId_ShouldReturnNull()
	{
		var user = await new UserBuilder(context).CreateAsync();
		var anotherUser = await new UserBuilder(context).CreateAsync();
		var session = await new TelegramSessionBuilder(context)
			.WithUserId(user.Id)
			.CreateAsync();

		var result = await sut.GetByIdAsync(anotherUser.Id, session.Id, CancellationToken.None);

		result.ShouldBeNull();
	}

	[Fact]
	public async Task GetByIdAsync_WithNonExistentSession_ShouldReturnNull()
	{
		var user = await new UserBuilder(context).CreateAsync();

		var result = await sut.GetByIdAsync(user.Id, Guid.NewGuid(), CancellationToken.None);

		result.ShouldBeNull();
	}

	[Fact]
	public async Task UpdateAsync_WithValidData_ShouldUpdateSession()
	{
		var user = await new UserBuilder(context).CreateAsync();
		var session = await new TelegramSessionBuilder(context)
			.WithUserId(user.Id)
			.CreateAsync();
		var newName = "Updated Name";
		var newIsActive = false;

		await sut.UpdateAsync(session.Id, newName, newIsActive, CancellationToken.None);
		context.ChangeTracker.Clear();
		var updated = await context.TelegramSessions
			.FirstAsync(s => s.Id == session.Id, CancellationToken.None);

		updated.Name.ShouldBe(newName);
		updated.IsActive.ShouldBe(newIsActive);
	}

	[Fact]
	public async Task UpdateAsync_WithNullName_ShouldSetNameToNull()
	{
		var user = await new UserBuilder(context).CreateAsync();
		var session = await new TelegramSessionBuilder(context)
			.WithUserId(user.Id)
			.CreateAsync();

		await sut.UpdateAsync(session.Id, null, true, CancellationToken.None);
		context.ChangeTracker.Clear();
		var updated = await context.TelegramSessions
			.FirstAsync(s => s.Id == session.Id, CancellationToken.None);

		updated.Name.ShouldBeNull();
		updated.IsActive.ShouldBeTrue();
	}

	[Fact]
	public async Task UpdateAsync_WithNonExistentSession_ShouldThrow()
	{
		await Should.ThrowAsync<InvalidOperationException>(async () =>
		{
			await sut.UpdateAsync(Guid.NewGuid(), "Test", true, CancellationToken.None);
		});
	}
}