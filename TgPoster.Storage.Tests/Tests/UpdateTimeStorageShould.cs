using Microsoft.EntityFrameworkCore;
using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Storages;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public class UpdateTimeStorageShould : IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context;
	private readonly UpdateTimeStorage sut;

	public UpdateTimeStorageShould(StorageTestFixture fixture)
	{
		context = fixture.GetDbContext();
		sut = new(context, new GuidFactory());
	}

	[Fact]
	public async Task UpdateTimeDay_WithValidData_ShouldUpdateTime()
	{
		var day = await new DayBuilder(context).CreateAsync();
		var time = new List<TimeOnly>
		{
			new(15, 12),
			new(15, 12),
			new(15, 14)
		};
		await sut.UpdateTimeDayAsync(day.Id, time, CancellationToken.None);

		var updDay = await context.Days.FirstOrDefaultAsync(x => x.Id == day.Id);
		updDay!.TimePostings.ShouldBe(time);
	}
}