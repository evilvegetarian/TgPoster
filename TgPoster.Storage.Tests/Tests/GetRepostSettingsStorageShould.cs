using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Storages.Repost;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public class GetRepostSettingsStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context = fixture.GetDbContext();
	private readonly CancellationToken ct = CancellationToken.None;
	private readonly GetRepostSettingsStorage sut = new(fixture.GetDbContext());

	[Fact]
	public async Task GetAsync_WithExistingIdAndUserId_ShouldReturnRepostSettings()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync(ct);
		var telegramSession = await new TelegramSessionBuilder(context)
			.WithUserId(schedule.UserId)
			.CreateAsync(ct);
		var repostSettings = await new RepostSettingsBuilder(context)
			.WithSchedule(schedule)
			.WithTelegramSession(telegramSession)
			.CreateAsync(ct);

		var response = await sut.GetAsync(repostSettings.Id, schedule.UserId, ct);

		response.ShouldNotBeNull();
		response.Id.ShouldBe(repostSettings.Id);
		response.ScheduleId.ShouldBe(schedule.Id);
		response.ScheduleName.ShouldBe(schedule.Name);
		response.TelegramSessionId.ShouldBe(telegramSession.Id);
		response.TelegramSessionName.ShouldBe(telegramSession.Name);
		response.IsActive.ShouldBe(repostSettings.IsActive);
		response.Destinations.ShouldBeEmpty();
	}

	[Fact]
	public async Task GetAsync_WithDestinations_ShouldReturnDestinationsList()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync(ct);
		var telegramSession = await new TelegramSessionBuilder(context)
			.WithUserId(schedule.UserId)
			.CreateAsync(ct);
		var repostSettings = await new RepostSettingsBuilder(context)
			.WithSchedule(schedule)
			.WithTelegramSession(telegramSession)
			.CreateAsync(ct);

		var destination1 = await new RepostDestinationBuilder(context)
			.WithRepostSettings(repostSettings)
			.WithChatIdentifier(-1001234567890)
			.WithIsActive(true)
			.CreateAsync(ct);

		var destination2 = await new RepostDestinationBuilder(context)
			.WithRepostSettings(repostSettings)
			.WithChatIdentifier(-1009876543210)
			.WithIsActive(false)
			.CreateAsync(ct);

		var response = await sut.GetAsync(repostSettings.Id, schedule.UserId, ct);

		response.ShouldNotBeNull();
		response.Destinations.Count.ShouldBe(2);
		response.Destinations.ShouldContain(d => d.Id == destination1.Id && d.ChatId == -1001234567890 && d.IsActive);
		response.Destinations.ShouldContain(d => d.Id == destination2.Id && d.ChatId == -1009876543210 && !d.IsActive);
	}

	[Fact]
	public async Task GetAsync_WithNonExistingId_ShouldReturnNull()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync(ct);
		await new RepostSettingsBuilder(context)
			.WithSchedule(schedule)
			.CreateAsync(ct);

		var response = await sut.GetAsync(Guid.NewGuid(), schedule.UserId, ct);

		response.ShouldBeNull();
	}

	[Fact]
	public async Task GetAsync_WithDifferentUserId_ShouldReturnNull()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync(ct);
		var repostSettings = await new RepostSettingsBuilder(context)
			.WithSchedule(schedule)
			.CreateAsync(ct);

		var response = await sut.GetAsync(repostSettings.Id, Guid.NewGuid(), ct);

		response.ShouldBeNull();
	}

	[Fact]
	public async Task GetAsync_WithNonExistingIdAndUserId_ShouldReturnNull()
	{
		var response = await sut.GetAsync(Guid.NewGuid(), Guid.NewGuid(), ct);

		response.ShouldBeNull();
	}
}
