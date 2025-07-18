using Microsoft.EntityFrameworkCore;
using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Storages;

namespace TgPoster.Storage.Tests.Tests;

public class UpdateStatusScheduleStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
    private readonly PosterContext context = fixture.GetDbContext();
    private readonly Helper helper = new(fixture.GetDbContext());
    private readonly UpdateStatusScheduleStorage sut = new(fixture.GetDbContext());

    [Fact]
    public async Task ExistSchedule_WithExistingSchedule_ShouldReturnTrue()
    {
        var schedule = await helper.CreateScheduleAsync();

        var result = await sut.ExistSchedule(schedule.Id, schedule.UserId, CancellationToken.None);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task ExistSchedule_WithNonExistingSchedule_ShouldReturnFalse()
    {
        var nonExistingScheduleId = Guid.NewGuid();

        var result = await sut.ExistSchedule(nonExistingScheduleId, Guid.NewGuid(), CancellationToken.None);

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task UpdateStatus_WithActiveSchedule_ShouldMakeItInactive()
    {
        var schedule = await helper.CreateScheduleAsync();

        await sut.UpdateStatus(schedule.Id, CancellationToken.None);

        var updatedSchedule = await context.Schedules.FindAsync(schedule.Id);
        updatedSchedule.ShouldNotBeNull();
        updatedSchedule.IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task UpdateStatus_WithInactiveSchedule_ShouldMakeItActive()
    {
        var schedule = await helper.CreateScheduleAsync();
        schedule.IsActive = false;
        context.Schedules.Update(schedule);
        await context.SaveChangesAsync();

        await sut.UpdateStatus(schedule.Id, CancellationToken.None);
        var updatedSchedule = await context.Schedules
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == schedule.Id);
        updatedSchedule.ShouldNotBeNull();
        updatedSchedule.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task UpdateStatus_WithNonExistingSchedule_ShouldThrowException()
    {
        var nonExistingScheduleId = Guid.NewGuid();

        var exception = await Should.ThrowAsync<NullReferenceException>(async () =>
            await sut.UpdateStatus(nonExistingScheduleId, CancellationToken.None));

        exception.ShouldNotBeNull();
    }

    [Fact]
    public async Task UpdateStatus_ShouldToggleStatusMultipleTimes()
    {
        var schedule = await helper.CreateScheduleAsync();
        var originalStatus = schedule.IsActive;

        await sut.UpdateStatus(schedule.Id, CancellationToken.None);
        var firstUpdate = await context.Schedules
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == schedule.Id);
        firstUpdate!.IsActive.ShouldBe(!originalStatus);

        await sut.UpdateStatus(schedule.Id, CancellationToken.None);
        var secondUpdate = await context.Schedules.FindAsync(schedule.Id);
        secondUpdate!.IsActive.ShouldBe(originalStatus);
    }
}