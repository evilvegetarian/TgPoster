namespace TgPoster.API.Domain.UseCases.Days.CreateDays;

public sealed record DayOfWeekForm(
    DayOfWeek DayOfWeekPosting,
    TimeOnly StartPosting,
    TimeOnly EndPosting,
    byte Interval
);