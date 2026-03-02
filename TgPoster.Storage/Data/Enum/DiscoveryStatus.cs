namespace TgPoster.Storage.Data.Enum;

/// <summary>
///     Статус обнаружения канала.
/// </summary>
public enum DiscoveryStatus
{
	/// <summary>
	///     Ожидает обработки.
	/// </summary>
	Pending = 0,

	/// <summary>
	///     В процессе парсинга.
	/// </summary>
	InProgress = 1,

	/// <summary>
	///     Парсинг завершён.
	/// </summary>
	Completed = 2,

	/// <summary>
	///     Ошибка при парсинге.
	/// </summary>
	Error = 3,

	/// <summary>
	///     Пропущен (бот, уже обработан и т.д.).
	/// </summary>
	Skipped = 4
}
