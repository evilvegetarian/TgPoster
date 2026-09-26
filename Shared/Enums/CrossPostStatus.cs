namespace Shared.Enums;

/// <summary>
///     Статус кросс-поста в очереди и журнале
/// </summary>
public enum CrossPostStatus
{
	/// <summary>
	///     Ожидает публикации
	/// </summary>
	Pending = 0,

	/// <summary>
	///     В процессе публикации
	/// </summary>
	InProgress = 1,

	/// <summary>
	///     Успешно опубликован
	/// </summary>
	Published = 2,

	/// <summary>
	///     Не удалось опубликовать
	/// </summary>
	Failed = 3,

	/// <summary>
	///     Осознанно пропущен
	/// </summary>
	Skipped = 4
}
