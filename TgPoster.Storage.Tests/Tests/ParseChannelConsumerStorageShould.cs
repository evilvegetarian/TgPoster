using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Storages;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public sealed class ParseChannelConsumerStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context = fixture.GetDbContext();
	private readonly ParseChannelConsumerStorage sut = new(fixture.GetDbContext());

	[Fact]
	public async Task GetScheduleIdAsync_ShouldReturnScheduleOfParsingSettings()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var cpp = await new ChannelParsingSettingBuilder(context).WithScheduleId(schedule.Id).CreateAsync();

		var result = await sut.GetScheduleIdAsync(cpp.Id, CancellationToken.None);

		result.ShouldBe(schedule.Id);
	}

	[Fact]
	public async Task GetScheduleIdAsync_ShouldReturnNull_WhenParsingSettingsNotFound()
	{
		var result = await sut.GetScheduleIdAsync(Guid.NewGuid(), CancellationToken.None);

		result.ShouldBeNull();
	}
}
