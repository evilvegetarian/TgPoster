using Microsoft.EntityFrameworkCore;
using Shouldly;
using TgPoster.API.Domain.UseCases.Days.CreateDays;
using TgPoster.Storage.Data;
using TgPoster.Storage.Storages;

namespace TgPoster.Storage.Tests.Tests;

public class CreateDaysStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
    private readonly PosterContext context = fixture.GetDbContext();
    private readonly Helper helper = new(fixture.GetDbContext());
    private readonly CreateDaysStorage sut = new(fixture.GetDbContext(), new GuidFactory());

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
    public async Task CreateDaysAsync_WithValidDays_ShouldCreateDays()
    {
        var schedule = await helper.CreateScheduleAsync();
        var days = new List<CreateDayDto>
        {
            new()
            {
                ScheduleId = schedule.Id,
                DayOfWeek = DayOfWeek.Monday,
                TimePostings = [TimeOnly.Parse("09:00"), TimeOnly.Parse("18:00")]
            },
            new()
            {
                ScheduleId = schedule.Id,
                DayOfWeek = DayOfWeek.Tuesday,
                TimePostings = [TimeOnly.Parse("10:00")]
            }
        };

        await sut.CreateDaysAsync(days, CancellationToken.None);

        var createdDays = await context.Days
            .Where(x => x.ScheduleId == schedule.Id)
            .ToListAsync();

        createdDays.ShouldNotBeEmpty();
        createdDays.Count.ShouldBe(2);
        createdDays.ShouldContain(x => x.DayOfWeek == DayOfWeek.Monday);
        createdDays.ShouldContain(x => x.DayOfWeek == DayOfWeek.Tuesday);
    }

    [Fact]
    public async Task GetDayOfWeekAsync_WithExistingDays_ShouldReturnDaysOfWeek()
    {
        var schedule = await helper.CreateScheduleAsync();
        await helper.CreateDayAsync(schedule.Id, DayOfWeek.Monday);
        await helper.CreateDayAsync(schedule.Id, DayOfWeek.Tuesday);

        var result = await sut.GetDayOfWeekAsync(schedule.Id, CancellationToken.None);

        result.ShouldNotBeEmpty();
        result.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetDayOfWeekAsync_WithNonExistingSchedule_ShouldReturnEmptyList()
    {
        var nonExistingScheduleId = Guid.NewGuid();

        var result = await sut.GetDayOfWeekAsync(nonExistingScheduleId, CancellationToken.None);

        result.ShouldBeEmpty();
    }
}