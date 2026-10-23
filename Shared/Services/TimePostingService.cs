using TgPoster.Exceptions.BadRequest;

namespace Shared.Services;

public sealed class TimePostingService
{
	private const int FreeSlotsMaxDaysAhead = 400;

	public List<DateTimeOffset> GetTimeForPosting(
		int messageCount,
		Dictionary<DayOfWeek, List<TimeOnly>> scheduleTime,
		DateTimeOffset existMessageTimePosting
	)
	{
		if (scheduleTime.Count == 0)
			throw new NotFoundTimeException();

		var currentDateValue = existMessageTimePosting > DateTimeOffset.UtcNow
			? existMessageTimePosting
			: DateTimeOffset.UtcNow;

		var currentDayOfWeek = currentDateValue.DayOfWeek;
		var currentTime = currentDateValue.TimeOfDay;

		var dateTimes = new List<DateTimeOffset>();
		var index = 0;

		while (index < messageCount)
		{
			if (scheduleTime.TryGetValue(currentDayOfWeek, out var timesForToday))
			{
				timesForToday.Sort();
				foreach (var time in timesForToday)
				{
					var timeSpan = time.ToTimeSpan();
					var potentialNewDateTime = new DateTimeOffset(currentDateValue.Date + timeSpan, TimeSpan.Zero);

					if (timeSpan > currentTime)
					{
						dateTimes.Add(potentialNewDateTime);
						index++;
						if (index >= messageCount)
						{
							break;
						}
					}
				}
			}

			currentDateValue = currentDateValue.AddDays(1);
			currentDayOfWeek = currentDateValue.DayOfWeek;
			currentTime = TimeSpan.Zero;
		}

		return dateTimes;
	}

	public List<DateTimeOffset> GetTimeForPosting(
		int messageCount,
		Dictionary<DayOfWeek, List<TimeOnly>> scheduleTime,
		List<DateTimeOffset> existMessageTimePosting
	)
	{
		var lastTime = existMessageTimePosting.OrderByDescending(x => x).FirstOrDefault();
		return GetTimeForPosting(messageCount, scheduleTime, lastTime);
	}

	public List<DateTimeOffset> GetFreeSlots(
		int count,
		Dictionary<DayOfWeek, List<TimeOnly>> scheduleTime,
		IReadOnlyCollection<DateTimeOffset> occupied,
		DateTimeOffset from
	)
	{
		if (scheduleTime.Count == 0)
			throw new NotFoundTimeException();

		var taken = occupied.Select(ToMinute).ToHashSet();
		var slots = new List<DateTimeOffset>();
		var startDate = from.UtcDateTime.Date;

		for (var day = 0; day <= FreeSlotsMaxDaysAhead && slots.Count < count; day++)
		{
			var date = startDate.AddDays(day);
			if (!scheduleTime.TryGetValue(date.DayOfWeek, out var times))
				continue;

			foreach (var time in times.OrderBy(x => x))
			{
				var slot = new DateTimeOffset(date + time.ToTimeSpan(), TimeSpan.Zero);
				if (slot <= from || !taken.Add(ToMinute(slot)))
					continue;

				slots.Add(slot);
				if (slots.Count == count)
					break;
			}
		}

		return slots;
	}

	private static long ToMinute(DateTimeOffset value) => value.UtcTicks / TimeSpan.TicksPerMinute;
}