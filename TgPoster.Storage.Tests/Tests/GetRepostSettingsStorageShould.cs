using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Storages.Repost;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public sealed class GetRepostSettingsStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context = fixture.GetDbContext();
	private readonly GetRepostSettingsStorage sut = new(fixture.GetDbContext());

	[Fact]
	public async Task GetRepostSettingsByScheduleIdAsync_WithExistingSettings_ShouldReturnSettings()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var session = await new TelegramSessionBuilder(context).CreateAsync();
		var settings = await new RepostSettingsBuilder(context)
			.WithScheduleId(schedule.Id)
			.WithTelegramSessionId(session.Id)
			.CreateAsync();

		await new RepostDestinationBuilder(context)
			.WithRepostSettingsId(settings.Id)
			.WithChatIdentifier("@channel1")
			.CreateAsync();

		await new RepostDestinationBuilder(context)
			.WithRepostSettingsId(settings.Id)
			.WithChatIdentifier("@channel2")
			.CreateAsync();

		var result = await sut.GetRepostSettingsByScheduleIdAsync(schedule.Id, CancellationToken.None);

		result.ShouldNotBeNull();
		result.Id.ShouldBe(settings.Id);
		result.ScheduleId.ShouldBe(schedule.Id);
		result.TelegramSessionId.ShouldBe(session.Id);
		result.Destinations.Count.ShouldBe(2);
		result.Destinations.ShouldContain(d => d.ChatIdentifier == "@channel1");
		result.Destinations.ShouldContain(d => d.ChatIdentifier == "@channel2");
	}

	[Fact]
	public async Task GetRepostSettingsByScheduleIdAsync_WithNonExistingSettings_ShouldReturnNull()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();

		var result = await sut.GetRepostSettingsByScheduleIdAsync(schedule.Id, CancellationToken.None);

		result.ShouldBeNull();
	}
}
