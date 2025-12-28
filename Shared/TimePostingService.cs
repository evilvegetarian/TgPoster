using Shared.SharedException;

namespace Shared;

public sealed class TimePostingService
{
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
}