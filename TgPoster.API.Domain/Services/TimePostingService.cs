namespace TgPoster.API.Domain.Services;

internal sealed class TimePostingService
{
    public List<DateTimeOffset> GetTimeForPosting(
        int mediaCount,
        Dictionary<DayOfWeek, List<TimeOnly>> scheduleTime,
        List<DateTimeOffset> existMessageTimePosting
    )
    {
        if (!scheduleTime.Any())
            throw new ArgumentNullException("Расписание не заполнено!");
        var currentDateValue = DateTimeOffset.UtcNow;

        var currentDayOfWeek = currentDateValue.DayOfWeek;
        var currentTime = currentDateValue.TimeOfDay;

        var dateTimes = new List<DateTimeOffset>();
        var index = 0;

        while (index < mediaCount)
        {
            if (scheduleTime.TryGetValue(currentDayOfWeek, out var timesForToday))
            {
                timesForToday.Sort();
                foreach (var time in timesForToday)
                {
                    var timeSpan = time.ToTimeSpan();
                    var potentialNewDateTime = new DateTimeOffset(currentDateValue.Date + timeSpan, TimeSpan.Zero);

                    if (timeSpan > currentTime && !existMessageTimePosting.Contains(potentialNewDateTime))
                    {
                        dateTimes.Add(potentialNewDateTime);
                        index++;
                        if (index >= mediaCount)
                            break;
                    }
                }
            }

            currentDateValue = currentDateValue.AddDays(1);
            currentDayOfWeek = currentDateValue.DayOfWeek;
            currentTime = TimeSpan.Zero;
        }

        return dateTimes;
    }
}