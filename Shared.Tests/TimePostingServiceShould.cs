using Shared.Exceptions;
using Shared.Services;
using Shouldly;

namespace Shared.Tests;

public sealed class TimePostingServiceShould
{
	private readonly TimePostingService sut = new();

	[Fact]
	public void GetTimeForPosting_ScheduleTimeNull_ThrowsException()
	{
		var scheduleTime = new Dictionary<DayOfWeek, List<TimeOnly>>();
		var existingTimes = new List<DateTimeOffset>();
		Should.Throw<SharedException>(() => sut.GetTimeForPosting(5, scheduleTime, existingTimes));
	}

	[Fact]
	public void GetTimeForPosting_ForCurrentDay_ReturnPostingTimes()
	{
		var now = DateTimeOffset.UtcNow;
		var currentDay = now.DayOfWeek;

		var currentTimeOnly = TimeOnly.FromDateTime(now.UtcDateTime);
		var futureTime = currentTimeOnly.Add(TimeSpan.FromMinutes(22));
		var futureTime2 = currentTimeOnly.Add(TimeSpan.FromMinutes(14));

		var scheduleTime = new Dictionary<DayOfWeek, List<TimeOnly>>
		{
			{ currentDay, [futureTime2, futureTime] }
		};

		var existingTimes = new List<DateTimeOffset>();

		var result = sut.GetTimeForPosting(2, scheduleTime, existingTimes);

		result.Count.ShouldBe(2);
		result.Select(x => x.TimeOfDay).ShouldContain(futureTime.ToTimeSpan());
		result.Select(x => x.TimeOfDay).ShouldContain(futureTime2.ToTimeSpan());
		foreach (var dt in result)
		{
			now.Date.ShouldBe(dt.Date);
			dt.TimeOfDay.ShouldBeGreaterThan(now.TimeOfDay);
		}
	}

	[Fact]
	public void ReturnPostingTimes_AcrossMultipleDays_WhenNotEnoughTimesInCurrentDay()
	{
		var now = DateTimeOffset.UtcNow;
		var currentDay = now.DayOfWeek;
		var nextDay = now.AddDays(1).DayOfWeek;

		// Для текущего дня выберем время, которое возможно уже прошло – чтобы гарантированно не попасть в список.
		// Например, выберем время через 1 мин назад (если возможно) или время, которое не сработает,
		// а для следующего дня – допустим любое время.
		var pastTime = TimeOnly.FromDateTime(now.UtcDateTime).Add(-TimeSpan.FromMinutes(1));
		// Если уменьшение даёт отрицательный TimeSpan, используем 00:00.
		if (pastTime.Minute < 0 || pastTime.Hour < 0)
		{
			pastTime = new TimeOnly(0, 0);
		}

		// Для следующего дня выберем два времени.
		var postingTime1 = new TimeOnly(10, 0);
		var postingTime2 = new TimeOnly(15, 0);

		var scheduleTime = new Dictionary<DayOfWeek, List<TimeOnly>>
		{
			// В текущий день время не подходит (до now либо равно)
			{ currentDay, new List<TimeOnly> { pastTime } },
			// Для следующего дня есть корректные времена
			{ nextDay, new List<TimeOnly> { postingTime1, postingTime2 } }
		};

		var existingTimes = new List<DateTimeOffset>();

		// Act
		var result = sut.GetTimeForPosting(2, scheduleTime, existingTimes);

		// Assert
		Assert.Equal(2, result.Count);
		// Оба времени должны быть во вторник (если сегодня не понедельник) или просто следующего дня относительно now.
		var expectedDay = now.AddDays(1).Date;
		foreach (var dt in result)
		{
			Assert.Equal(expectedDay, dt.Date);
		}
	}

	[Fact]
	public void SkipPostingTime_ThatAlreadyExistsInExistingTimes()
	{
		var now = DateTimeOffset.UtcNow;
		var currentDay = now.DayOfWeek;

		var postingTime = TimeOnly.FromDateTime(now.UtcDateTime).Add(TimeSpan.FromMinutes(10));
		var scheduleTime = new Dictionary<DayOfWeek, List<TimeOnly>>
		{
			{ currentDay, [postingTime] }
		};

		var expectedPostingDateTime = new DateTimeOffset(now.Date + postingTime.ToTimeSpan(), TimeSpan.Zero);

		var existingTimes = new List<DateTimeOffset> { expectedPostingDateTime };

		var result = sut.GetTimeForPosting(1, scheduleTime, existingTimes);

		result.Count.ShouldBe(1);
		var resultTime = result.First();
		resultTime.Date.ShouldBe(now.AddDays(7).Date);
		resultTime.TimeOfDay.ShouldBe(postingTime.ToTimeSpan());
	}
}