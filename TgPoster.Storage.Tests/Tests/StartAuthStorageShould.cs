using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Storages;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public sealed class StartAuthStorageShould(StorageTestFixture fixture)
	: IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context = fixture.GetDbContext();
	private readonly StartAuthStorage sut = new(fixture.GetDbContext());

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
}
