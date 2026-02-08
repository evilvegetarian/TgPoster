using Bogus;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Tests.Builders;

internal class DayBuilder(PosterContext context)
{
	private static readonly Faker faker = FakerProvider.Instance;

	private readonly Day day = new()
	{
		Id = Guid.NewGuid(),
		DayOfWeek = faker.Random.Enum<DayOfWeek>(),
		ScheduleId = new ScheduleBuilder(context).Create().Id
	};

	public Day Build() => day;

	public Day Create()
	{
		context.Days.AddRange(day);
		context.SaveChanges();
		return day;
	}

	public async Task<Day> CreateAsync(CancellationToken ct = default)
	{
		await context.Days.AddRangeAsync(day);
		await context.SaveChangesAsync(ct);
		return day;
	}

	public DayBuilder WithScheduleId(Guid scheduleId)
	{
		day.ScheduleId = scheduleId;
		return this;
	}

	public DayBuilder WithDayOfWeek(DayOfWeek dayOfWeek)
	{
		day.DayOfWeek = dayOfWeek;
		return this;
	}

	public DayBuilder WithTimePostings(IEnumerable<TimeOnly> timePostings)
	{
		day.TimePostings = timePostings.ToList();
		return this;
	}
}