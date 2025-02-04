using Microsoft.EntityFrameworkCore;
using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Storages;

namespace TgPoster.Storage.Tests;

public class UpdateTimeStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
    private readonly UpdateTimeStorage sut = new(fixture.GetDbContext());
    private readonly PosterContext context = fixture.GetDbContext();
    private readonly Helper create=new (fixture.GetDbContext());

    [Fact]
    public async Task UpdateTimeDay_WithValid()
    {
        var day = await create.CreateDayAsync();
        var time = new List<TimeOnly>()
        {
            new TimeOnly(15, 12),
            new TimeOnly(15, 12),
            new TimeOnly(15, 14)
        };
        await sut.UpdateTimeDayAsync(day.Id, time, CancellationToken.None);
        
        var updDay=await context.Days.FirstOrDefaultAsync(x=>x.Id == day.Id);
        updDay!.TimePostings.ShouldBe(time);
    }
}