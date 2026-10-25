namespace Shared.Discover;

/// <summary>
/// Значения настроек поиска каналов (Discover) по умолчанию: ими засевается запись в БД. Совпадают с тем,
/// как воркер работал до появления настроек
/// </summary>
public static class DiscoverDefaults
{
	/// <summary>Включён ли поиск каналов</summary>
	public const bool IsEnabled = true;

	/// <summary>Как часто запускать поиск каналов, в минутах</summary>
	public const int IntervalMinutes = 60;

	/// <summary>Сколько каналов парсит каждая сессия за один запуск</summary>
	public const int ChannelsPerSession = 1;
}
