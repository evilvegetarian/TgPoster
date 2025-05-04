using Microsoft.EntityFrameworkCore;
using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Storages;

namespace TgPoster.Storage.Tests;

public class DeleteScheduleStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
    private readonly PosterContext context = fixture.GetDbContext();
    private readonly Helper helper = new(fixture.GetDbContext());
    private readonly DeleteScheduleStorage sut = new(fixture.GetDbContext());

    [Fact]
    public async Task ScheduleExist_WithExistingSchedule_ShouldReturnTrue()
    {
        var schedule = await helper.CreateScheduleAsync();
        var exist = await sut.ScheduleExistAsync(schedule.Id, schedule.UserId);
        exist.ShouldBeTrue();
    }

    [Fact]
    public async Task ScheduleExist_WithNonExistingSchedule_ShouldReturnFalse()
    {
        var exist = await sut.ScheduleExistAsync(Guid.Parse("085bd737-a992-4382-931d-548ea6460ffd"),
            Guid.Parse("9b6223b3-3e21-495e-a2d7-79867345de07"));
        exist.ShouldBeFalse();
    }

    [Fact]
    public async Task DeleteSchedule_WithValidId_ShouldMarkAsDeleted()
    {
        var newSchedule = await helper.CreateScheduleAsync();
        await sut.DeleteScheduleAsync(newSchedule.Id);
        var schedule = await context.Schedules
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == newSchedule.Id);
        schedule!.Deleted.ShouldNotBeNull();
    }
}
