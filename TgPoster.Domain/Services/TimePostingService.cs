namespace TgPoster.Domain.Services;

public class TimePostingService
{
    public List<DateTime> GetTimeForPosting(
        int mediaCount,
        Dictionary<DayOfWeek, List<TimeOnly>> scheduleTime,
        List<DateTime> existMessageTimePosting
    )
    {
        var currentDateValue = DateTime.Now;

        var currentDayOfWeek = currentDateValue.DayOfWeek;
        var currentTime = currentDateValue.TimeOfDay;

        var dateTimes = new List<DateTime>();
        int index = 0;

        while (index < mediaCount)
        {
            if (scheduleTime.TryGetValue(currentDayOfWeek, out var timesForToday))
            {
                timesForToday.Sort();
                foreach (var time in timesForToday)
                {
                    var timeSpan = time.ToTimeSpan();
                    var potentialNewDateTime = currentDateValue.Date + timeSpan;

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