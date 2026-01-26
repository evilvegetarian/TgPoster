using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Storages.Repost;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public sealed class ListRepostSettingsStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context = fixture.GetDbContext();
	private readonly ListRepostSettingsStorage sut = new(fixture.GetDbContext());

	[Fact]
	public async Task GetListAsync_WithExistingSettings_ShouldReturnUserSettings()
	{
		var user = await new UserBuilder(context).CreateAsync();
		var telegramBot = new TelegramBotBuilder(context).WithOwnerId(user.Id).Create();
		var schedule = await new ScheduleBuilder(context)
			.WithUserId(user.Id)
			.WithTelegramBotId(telegramBot.Id)
			.CreateAsync();
		var session = await new TelegramSessionBuilder(context).CreateAsync();
		var settings = await new RepostSettingsBuilder(context)
			.WithScheduleId(schedule.Id)
			.WithTelegramSessionId(session.Id)
			.CreateAsync();

		await new RepostDestinationBuilder(context)
			.WithRepostSettingsId(settings.Id)
			.CreateAsync();
		await new RepostDestinationBuilder(context)
			.WithRepostSettingsId(settings.Id)
			.CreateAsync();

		var result = await sut.GetListAsync(user.Id, CancellationToken.None);

		result.ShouldNotBeNull();
		result.Count.ShouldBe(1);
		var item = result[0];
		item.Id.ShouldBe(settings.Id);
		item.ScheduleId.ShouldBe(schedule.Id);
		item.ScheduleName.ShouldBe(schedule.Name);
		item.TelegramSessionId.ShouldBe(session.Id);
		item.IsActive.ShouldBeTrue();
		item.DestinationsCount.ShouldBe(2);
	}

	[Fact]
	public async Task GetListAsync_WithNoSettings_ShouldReturnEmptyList()
	{
		var user = await new UserBuilder(context).CreateAsync();

		var result = await sut.GetListAsync(user.Id, CancellationToken.None);

		result.ShouldNotBeNull();
		result.ShouldBeEmpty();
	}

	[Fact]
	public async Task GetListAsync_ShouldNotReturnOtherUserSettings()
	{
		var user1 = await new UserBuilder(context).CreateAsync();
		var user2 = await new UserBuilder(context).CreateAsync();

		var telegramBot1 = new TelegramBotBuilder(context).WithOwnerId(user1.Id).Create();
		var telegramBot2 = new TelegramBotBuilder(context).WithOwnerId(user2.Id).Create();

		var schedule1 = await new ScheduleBuilder(context)
			.WithUserId(user1.Id)
			.WithTelegramBotId(telegramBot1.Id)
			.CreateAsync();
		var schedule2 = await new ScheduleBuilder(context)
			.WithUserId(user2.Id)
			.WithTelegramBotId(telegramBot2.Id)
			.CreateAsync();

		var session = await new TelegramSessionBuilder(context).CreateAsync();

		await new RepostSettingsBuilder(context)
			.WithScheduleId(schedule1.Id)
			.WithTelegramSessionId(session.Id)
			.CreateAsync();
		await new RepostSettingsBuilder(context)
			.WithScheduleId(schedule2.Id)
			.WithTelegramSessionId(session.Id)
			.CreateAsync();

		var result = await sut.GetListAsync(user1.Id, CancellationToken.None);

		result.ShouldNotBeNull();
		result.Count.ShouldBe(1);
		result[0].ScheduleId.ShouldBe(schedule1.Id);
	}

	[Fact]
	public async Task GetListAsync_WithMultipleSettings_ShouldReturnAll()
	{
		var user = await new UserBuilder(context).CreateAsync();
		var telegramBot = new TelegramBotBuilder(context).WithOwnerId(user.Id).Create();
		var session = await new TelegramSessionBuilder(context).CreateAsync();

		var schedule1 = await new ScheduleBuilder(context)
			.WithUserId(user.Id)
			.WithTelegramBotId(telegramBot.Id)
			.CreateAsync();
		var schedule2 = await new ScheduleBuilder(context)
			.WithUserId(user.Id)
			.WithTelegramBotId(telegramBot.Id)
			.CreateAsync();

		var settings1 = await new RepostSettingsBuilder(context)
			.WithScheduleId(schedule1.Id)
			.WithTelegramSessionId(session.Id)
			.WithIsActive(true)
			.CreateAsync();
		var settings2 = await new RepostSettingsBuilder(context)
			.WithScheduleId(schedule2.Id)
			.WithTelegramSessionId(session.Id)
			.WithIsActive(false)
			.CreateAsync();

		var result = await sut.GetListAsync(user.Id, CancellationToken.None);

		result.ShouldNotBeNull();
		result.Count.ShouldBe(2);
		result.ShouldContain(x => x.Id == settings1.Id && x.IsActive);
		result.ShouldContain(x => x.Id == settings2.Id && !x.IsActive);
	}
}
