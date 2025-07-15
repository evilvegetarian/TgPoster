using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Storages;

namespace TgPoster.Storage.Tests.Tests;

public class GetDaysStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
    private readonly Helper helper = new(fixture.GetDbContext());
    private readonly GetDaysStorage sut = new(fixture.GetDbContext());

    [Fact]
    public async Task ScheduleExistAsync_WithExistingSchedule_ShouldReturnTrue()
    {
        var schedule = await helper.CreateScheduleAsync();

        var result = await sut.ScheduleExistAsync(schedule.Id, schedule.UserId, CancellationToken.None);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task ScheduleExistAsync_WithNonExistingSchedule_ShouldReturnFalse()
    {
        var user = await helper.CreateUserAsync();
        var nonExistingScheduleId = Guid.NewGuid();

        var result = await sut.ScheduleExistAsync(nonExistingScheduleId, user.Id, CancellationToken.None);

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task ScheduleExistAsync_WithWrongUserId_ShouldReturnFalse()
    {
        var schedule = await helper.CreateScheduleAsync();
        var wrongUserId = Guid.NewGuid();

        var result = await sut.ScheduleExistAsync(schedule.Id, wrongUserId, CancellationToken.None);

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task GetDaysAsync_WithExistingDays_ShouldReturnDays()
    {
        var schedule = await helper.CreateScheduleAsync();
        var day1 = await helper.CreateDayAsync(schedule.Id);
        var day2 = await helper.CreateDayAsync(schedule.Id);

        var result = await sut.GetDaysAsync(schedule.Id, CancellationToken.None);

        result.ShouldNotBeEmpty();
        result.Count.ShouldBe(2);
        result.ShouldContain(x => x.Id == day1.Id);
        result.ShouldContain(x => x.Id == day2.Id);
        result.All(x => x.ScheduleId == schedule.Id).ShouldBeTrue();
    }

    [Fact]
    public async Task GetDaysAsync_WithNonExistingSchedule_ShouldReturnEmptyList()
    {
        var nonExistingScheduleId = Guid.NewGuid();

        var result = await sut.GetDaysAsync(nonExistingScheduleId, CancellationToken.None);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetDaysAsync_WithScheduleWithoutDays_ShouldReturnEmptyList()
    {
        var schedule = await helper.CreateScheduleAsync();

        var result = await sut.GetDaysAsync(schedule.Id, CancellationToken.None);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetDaysAsync_ShouldReturnCorrectDayData()
    {
        var schedule = await helper.CreateScheduleAsync();
        var day = await helper.CreateDayAsync(schedule.Id);

        var result = await sut.GetDaysAsync(schedule.Id, CancellationToken.None);

        result.ShouldNotBeEmpty();
        var returnedDay = result.First();
        returnedDay.Id.ShouldBe(day.Id);
        returnedDay.ScheduleId.ShouldBe(day.ScheduleId);
        returnedDay.DayOfWeek.ShouldBe(day.DayOfWeek);
        returnedDay.TimePostings.ShouldBe(day.TimePostings);
    }
}
