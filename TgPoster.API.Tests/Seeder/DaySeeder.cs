using Microsoft.EntityFrameworkCore;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Endpoint.Tests.Seeder;

internal class DaySeeder(PosterContext context) : BaseSeeder
{
	public override async Task Seed()
	{
		if (await context.Days.AnyAsync())
		{
			return;
		}

		var days = new List<Day>
		{
			new()
			{
				Id = Guid.Parse("19c6e5ea-2fab-45d2-ad08-926b16fbf2c6"),
				ScheduleId = GlobalConst.Worked.ScheduleId,
				DayOfWeek = DayOfWeek.Monday,
				TimePostings =
				[
					new TimeOnly(12, 44),
					new TimeOnly(12, 48),
					new TimeOnly(12, 50)
				]
			},
			new()
			{
				Id = Guid.Parse("28ea57a7-21da-4859-9794-c587d82e06f7"),
				ScheduleId = GlobalConst.Worked.ScheduleId,
				DayOfWeek = DayOfWeek.Thursday,
				TimePostings =
				[
					new TimeOnly(15, 42),
					new TimeOnly(11, 48),
					new TimeOnly(22, 01)
				]
			}
		};
		await context.Days.AddRangeAsync(days);
	}
}