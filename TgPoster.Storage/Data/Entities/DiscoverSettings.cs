namespace TgPoster.Storage.Data.Entities;

/// <summary>
/// Настройки поиска каналов (Discover) — одна запись на всю систему
/// </summary>
public sealed class DiscoverSettings : BaseEntity
{
	/// <summary>
	/// Фиксированный ID единственной записи: одновременное создание из API и воркера упрётся в первичный ключ,
	/// а не породит дубликат
	/// </summary>
	public static readonly Guid SingletonId = new("3f8e2c71-5a4d-4b9e-8c06-d2a7b1e94f53");

	/// <summary>Включён ли поиск каналов</summary>
	public bool IsEnabled { get; set; }

	/// <summary>Как часто запускать поиск каналов, в минутах</summary>
	public int IntervalMinutes { get; set; }

	/// <summary>Сколько каналов парсит каждая сессия за один запуск</summary>
	public int ChannelsPerSession { get; set; }
}
