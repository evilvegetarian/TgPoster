using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Storages;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public class GetScheduleStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context = fixture.GetDbContext();
	private readonly GetScheduleStorage sut = new(fixture.GetDbContext());

	[Fact]
	public async Task GetScheduleAsync_WithExistingSchedule_ShouldReturnSchedule()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();

		var result = await sut.GetScheduleAsync(schedule.Id, schedule.UserId, CancellationToken.None);

		result.ShouldNotBeNull();
		result.Id.ShouldBe(schedule.Id);
		result.Name.ShouldBe(schedule.Name);
		result.IsActive.ShouldBe(schedule.IsActive);
	}

	[Fact]
	public async Task GetScheduleAsync_WithNonExistingSchedule_ShouldReturnNull()
	{
		var user = await new UserBuilder(context).CreateAsync();
		var nonExistingScheduleId = Guid.NewGuid();

		var result = await sut.GetScheduleAsync(nonExistingScheduleId, user.Id, CancellationToken.None);

		result.ShouldBeNull();
	}

	[Fact]
	public async Task GetScheduleAsync_WithWrongUserId_ShouldReturnNull()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var wrongUserId = Guid.NewGuid();

		var result = await sut.GetScheduleAsync(schedule.Id, wrongUserId, CancellationToken.None);

		result.ShouldBeNull();
	}

	[Fact]
	public async Task GetScheduleAsync_ShouldReturnTelegramBotName()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();

		var result = await sut.GetScheduleAsync(schedule.Id, schedule.UserId, CancellationToken.None);

		result.ShouldNotBeNull();
		result.ChannelName.ShouldNotBeNullOrEmpty();
	}
}