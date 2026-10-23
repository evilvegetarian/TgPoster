using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Storages;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public sealed class ScheduleAlertStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context = fixture.GetDbContext();
	private readonly ScheduleAlertStorage sut = new(fixture.GetDbContext());

	[Fact]
	public async Task GetAlertTargetAsync_ShouldReturnBotAndScheduleNames_WhenScheduleExists()
	{
		var user = await new UserBuilder(context).CreateAsync();
		var bot = await new TelegramBotBuilder(context)
			.WithOwnerId(user.Id)
			.WithApiTelegram("encrypted-token")
			.WithChatId(555)
			.CreateAsync();
		var schedule = await new ScheduleBuilder(context)
			.WithUserId(user.Id)
			.WithTelegramBotId(bot.Id)
			.CreateAsync();

		var result = await sut.GetAlertTargetAsync(schedule.Id, CancellationToken.None);

		result.ShouldNotBeNull();
		result.EncryptedBotToken.ShouldBe("encrypted-token");
		result.OwnerChatId.ShouldBe(555);
		result.ScheduleName.ShouldBe(schedule.Name);
		result.ChannelName.ShouldBe(schedule.ChannelName);
	}

	[Fact]
	public async Task GetAlertTargetAsync_ShouldReturnNull_WhenScheduleNotFound()
	{
		var result = await sut.GetAlertTargetAsync(Guid.NewGuid(), CancellationToken.None);

		result.ShouldBeNull();
	}
}
