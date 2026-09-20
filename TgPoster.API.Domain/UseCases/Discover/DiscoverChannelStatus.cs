namespace TgPoster.API.Domain.UseCases.Discover;

/// <summary>
///     Статус обработки обнаруженного канала, как он записан в хранилище
/// </summary>
public enum DiscoverChannelStatus
{
	/// <summary>
	///     Ожидает парсинга
	/// </summary>
	Pending = 0,

	/// <summary>
	///     В процессе парсинга
	/// </summary>
	InProgress = 1,

	/// <summary>
	///     Парсинг завершён
	/// </summary>
	Completed = 2,

	/// <summary>
	///     Ошибка при парсинге
	/// </summary>
	Error = 3,

	/// <summary>
	///     Пропущен
	/// </summary>
	Skipped = 4
}
