using Microsoft.EntityFrameworkCore;
using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Storages;

namespace TgPoster.Storage.Tests.Tests;

public class CreateScheduleStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context = fixture.GetDbContext();
	private readonly Helper helper = new(fixture.GetDbContext());
	private readonly CreateScheduleStorage sut = new(fixture.GetDbContext(), new GuidFactory());

	[Fact]
	public async Task CreateScheduleAsync_WithValidData_ShouldCreateSchedule()
	{
		var user = await helper.CreateUserAsync();
		var telegramBot = await helper.CreateTelegramBotAsync(user.Id);
		var name = "Test Schedule";
		var channelId = -1001234567890L;
		var channelName = "Test Channel";

		var scheduleId = await sut.CreateScheduleAsync(
			name,
			user.Id,
			telegramBot.Id,
			channelId,
			channelName,
			CancellationToken.None);

		scheduleId.ShouldNotBe(Guid.Empty);

		var createdSchedule = await context.Schedules
			.FirstOrDefaultAsync(x => x.Id == scheduleId);

		createdSchedule.ShouldNotBeNull();
		createdSchedule.Name.ShouldBe(name);
		createdSchedule.UserId.ShouldBe(user.Id);
		createdSchedule.TelegramBotId.ShouldBe(telegramBot.Id);
		createdSchedule.ChannelId.ShouldBe(channelId);
		createdSchedule.ChannelName.ShouldBe(channelName);
	}

	[Fact]
	public async Task GetApiTokenAsync_WithExistingTelegramBot_ShouldReturnApiToken()
	{
		var user = await helper.CreateUserAsync();
		var telegramBot = await helper.CreateTelegramBotAsync(user.Id);

		var result = await sut.GetApiTokenAsync(telegramBot.Id, user.Id, CancellationToken.None);

		result.ShouldNotBeNull();
		result.ShouldBe(telegramBot.ApiTelegram);
	}

	[Fact]
	public async Task GetApiTokenAsync_WithNonExistingTelegramBot_ShouldReturnNull()
	{
		var user = await helper.CreateUserAsync();
		var nonExistingBotId = Guid.NewGuid();

		var result = await sut.GetApiTokenAsync(nonExistingBotId, user.Id, CancellationToken.None);

		result.ShouldBeNull();
	}

	[Fact]
	public async Task GetApiTokenAsync_WithWrongUserId_ShouldReturnNull()
	{
		var user = await helper.CreateUserAsync();
		var telegramBot = await helper.CreateTelegramBotAsync(user.Id);
		var wrongUserId = Guid.NewGuid();

		var result = await sut.GetApiTokenAsync(telegramBot.Id, wrongUserId, CancellationToken.None);

		result.ShouldBeNull();
	}
}