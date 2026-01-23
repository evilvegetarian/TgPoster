using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Enum;
using TgPoster.Storage.Storages;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public sealed class ListTelegramSessionsStorageShould(StorageTestFixture fixture)
	: IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context = fixture.GetDbContext();
	private readonly ListTelegramSessionsStorage sut = new(fixture.GetDbContext());

	[Fact]
	public async Task GetByUserIdAsync_WithNoSessions_ShouldReturnEmptyList()
	{
		var user = await new UserBuilder(context).CreateAsync();

		var result = await sut.GetByUserIdAsync(user.Id, CancellationToken.None);

		result.ShouldBeEmpty();
	}

	[Fact]
	public async Task GetByUserIdAsync_WithSessions_ShouldReturnUserSessions()
	{
		var user = await new UserBuilder(context).CreateAsync();
		var session1 = await new TelegramSessionBuilder(context)
			.WithUserId(user.Id)
			.CreateAsync();
		var session2 = await new TelegramSessionBuilder(context)
			.WithUserId(user.Id)
			.CreateAsync();

		var result = await sut.GetByUserIdAsync(user.Id, CancellationToken.None);

		result.Count.ShouldBe(2);
		result.ShouldContain(s => s.Id == session1.Id);
		result.ShouldContain(s => s.Id == session2.Id);
	}

	[Fact]
	public async Task GetByUserIdAsync_ShouldReturnOnlyUserSessions()
	{
		var user1 = await new UserBuilder(context).CreateAsync();
		var user2 = await new UserBuilder(context).CreateAsync();
		var session1 = await new TelegramSessionBuilder(context)
			.WithUserId(user1.Id)
			.CreateAsync();
		var session2 = await new TelegramSessionBuilder(context)
			.WithUserId(user2.Id)
			.CreateAsync();

		var result = await sut.GetByUserIdAsync(user1.Id, CancellationToken.None);

		result.Count.ShouldBe(1);
		result[0].Id.ShouldBe(session1.Id);
		result.ShouldNotContain(s => s.Id == session2.Id);
	}

	[Fact]
	public async Task GetByUserIdAsync_ShouldOrderByCreatedDescending()
	{
		var user = await new UserBuilder(context).CreateAsync();
		var session1 = await new TelegramSessionBuilder(context)
			.WithUserId(user.Id)
			.WithCreated(DateTime.UtcNow.AddHours(-2))
			.CreateAsync();
		var session2 = await new TelegramSessionBuilder(context)
			.WithUserId(user.Id)
			.WithCreated(DateTime.UtcNow.AddHours(-1))
			.CreateAsync();
		var session3 = await new TelegramSessionBuilder(context)
			.WithUserId(user.Id)
			.WithCreated(DateTime.UtcNow)
			.CreateAsync();

		var result = await sut.GetByUserIdAsync(user.Id, CancellationToken.None);

		result.Count.ShouldBe(3);
		result[0].Id.ShouldBe(session3.Id);
		result[1].Id.ShouldBe(session2.Id);
		result[2].Id.ShouldBe(session1.Id);
	}

	[Fact]
	public async Task GetByUserIdAsync_ShouldReturnCorrectProperties()
	{
		var user = await new UserBuilder(context).CreateAsync();
		var session = await new TelegramSessionBuilder(context)
			.WithUserId(user.Id)
			.WithPhoneNumber("+79991234567")
			.WithName("Test Session")
			.WithIsActive(true)
			.WithStatus(TelegramSessionStatus.Authorized)
			.CreateAsync();

		var result = await sut.GetByUserIdAsync(user.Id, CancellationToken.None);

		result.Count.ShouldBe(1);
		var dto = result[0];
		dto.Id.ShouldBe(session.Id);
		dto.PhoneNumber.ShouldBe("+79991234567");
		dto.Name.ShouldBe("Test Session");
		dto.IsActive.ShouldBeTrue();
		dto.Status.ShouldBe(Shared.Telegram.TelegramSessionStatus.Authorized);
	}
}
