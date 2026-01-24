using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Storages;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public class GetTelegramBotStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context = fixture.GetDbContext();
	private readonly GetTelegramBotStorage sut = new(fixture.GetDbContext());

	[Fact]
	public async Task GetApiTokenAsync_WithExistingSchedule_ShouldReturnApiToken()
	{
		var telegramBot = await new TelegramBotBuilder(context).CreateAsync();

		var result = await sut.GetApiTokenAsync(telegramBot.Id, telegramBot.OwnerId, CancellationToken.None);

		result.ShouldNotBeNull();
	}

	[Fact]
	public async Task GetApiTokenAsync_WithNonExistingSchedule_ShouldReturnNull()
	{
		var user = await new UserBuilder(context).CreateAsync();
		var nonExistingScheduleId = Guid.NewGuid();

		var result = await sut.GetApiTokenAsync(nonExistingScheduleId, user.Id, CancellationToken.None);

		result.ShouldBeNull();
	}

	[Fact]
	public async Task GetApiTokenAsync_WithWrongUserId_ShouldReturnNull()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var wrongUserId = Guid.NewGuid();

		var result = await sut.GetApiTokenAsync(schedule.Id, wrongUserId, CancellationToken.None);

		result.ShouldBeNull();
	}
}