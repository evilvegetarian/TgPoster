using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.Enum;
using TgPoster.Storage.Storages;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public sealed class ScheduleQueueCheckStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context = fixture.GetDbContext();
	private readonly ScheduleQueueCheckStorage sut = new(fixture.GetDbContext());

	[Fact]
	public async Task GetActiveSchedulesQueueAsync_ShouldSkipInactiveAndWithoutTimeSlots()
	{
		var active = await CreateScheduleWithSlotsAsync();
		var inactive = await CreateScheduleWithSlotsAsync(isActive: false);
		var withEmptySlots = await new ScheduleBuilder(context).CreateAsync();
		await new DayBuilder(context).WithScheduleId(withEmptySlots.Id).WithTimePostings([]).CreateAsync();
		var withoutDays = await new ScheduleBuilder(context).CreateAsync();

		var result = await sut.GetActiveSchedulesQueueAsync(DateTimeOffset.UtcNow, CancellationToken.None);

		var ids = result.Select(x => x.ScheduleId).ToList();
		ids.ShouldContain(active.Id);
		ids.ShouldNotContain(inactive.Id);
		ids.ShouldNotContain(withEmptySlots.Id);
		ids.ShouldNotContain(withoutDays.Id);
	}

	[Fact]
	public async Task GetActiveSchedulesQueueAsync_ShouldCountOnlyVerifiedRegisterAndInHandleInFuture()
	{
		var now = DateTimeOffset.UtcNow;
		var schedule = await CreateScheduleWithSlotsAsync();
		CreateMessage(schedule, MessageStatus.Register, now.AddHours(1));
		var lastPlanned = CreateMessage(schedule, MessageStatus.InHandle, now.AddHours(3));
		CreateMessage(schedule, MessageStatus.Register, now.AddHours(-1));
		CreateMessage(schedule, MessageStatus.Send, now.AddHours(5));
		CreateMessage(schedule, MessageStatus.Error, now.AddHours(6));
		CreateMessage(schedule, MessageStatus.Register, now.AddHours(10), isVerified: false);

		var result = await sut.GetActiveSchedulesQueueAsync(now, CancellationToken.None);

		var queue = result.Single(x => x.ScheduleId == schedule.Id);
		queue.PlannedCount.ShouldBe(2);
		queue.LastPlannedAt.ShouldNotBeNull();
		queue.LastPlannedAt.Value.ShouldBe(lastPlanned.TimePosting, TimeSpan.FromMilliseconds(1));
		queue.AwaitingApprovalCount.ShouldBe(1);
	}

	[Fact]
	public async Task GetActiveSchedulesQueueAsync_ShouldReturnEmptyQueue_WhenNoPlannedPosts()
	{
		var schedule = await CreateScheduleWithSlotsAsync();

		var result = await sut.GetActiveSchedulesQueueAsync(DateTimeOffset.UtcNow, CancellationToken.None);

		var queue = result.Single(x => x.ScheduleId == schedule.Id);
		queue.PlannedCount.ShouldBe(0);
		queue.LastPlannedAt.ShouldBeNull();
		queue.AwaitingApprovalCount.ShouldBe(0);
	}

	private async Task<Schedule> CreateScheduleWithSlotsAsync(bool isActive = true)
	{
		var schedule = await new ScheduleBuilder(context).WithIsActive(isActive).CreateAsync();
		await new DayBuilder(context)
			.WithScheduleId(schedule.Id)
			.WithTimePostings([new TimeOnly(10, 0), new TimeOnly(18, 0)])
			.CreateAsync();
		return schedule;
	}

	private Message CreateMessage(Schedule schedule, MessageStatus status, DateTimeOffset timePosting,
		bool isVerified = true)
	{
		return new MessageBuilder(context)
			.WithScheduleId(schedule.Id)
			.WithStatus(status)
			.WithTimeMessage(timePosting)
			.WithIsVerified(isVerified)
			.Create();
	}
}
