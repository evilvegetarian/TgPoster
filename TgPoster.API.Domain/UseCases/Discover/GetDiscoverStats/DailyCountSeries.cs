namespace TgPoster.API.Domain.UseCases.Discover.GetDiscoverStats;

/// <summary>
///     Окно таймлайна «по дням» и развёртка разреженных счётчиков в плотный ряд
/// </summary>
/// <param name="FirstDay">Первый день окна (UTC)</param>
/// <param name="LastDay">Последний день окна — сегодня (UTC)</param>
internal sealed record DailyCountSeries(DateOnly FirstDay, DateOnly LastDay)
{
	/// <summary>
	///     Начало первого дня окна: с этого момента хранилище считает события
	/// </summary>
	public DateTimeOffset Since => new(FirstDay.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

	/// <summary>
	///     Окно из <paramref name="days" /> дней, заканчивающееся сегодняшним днём
	/// </summary>
	/// <param name="days"></param>
	/// <param name="now"></param>
	/// <returns></returns>
	public static DailyCountSeries EndingAt(int days, DateTimeOffset now)
	{
		var today = DateOnly.FromDateTime(now.UtcDateTime);
		return new DailyCountSeries(today.AddDays(-(days - 1)), today);
	}

	/// <summary>
	///     Развернуть разреженный список по дням в плотный: каждый день окна присутствует, пустые дни — с нулём
	/// </summary>
	/// <param name="counts"></param>
	/// <returns></returns>
	public List<DiscoverDailyCount> Fill(IEnumerable<DiscoverDailyCount> counts)
	{
		var byDate = counts
			.GroupBy(x => x.Date)
			.ToDictionary(g => g.Key, g => g.Sum(x => x.Count));

		var result = new List<DiscoverDailyCount>(LastDay.DayNumber - FirstDay.DayNumber + 1);
		for (var day = FirstDay; day <= LastDay; day = day.AddDays(1))
		{
			result.Add(new DiscoverDailyCount
			{
				Date = day,
				Count = byDate.GetValueOrDefault(day)
			});
		}

		return result;
	}
}
