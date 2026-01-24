namespace TgPoster.Storage.Data.Enum;

/// <summary>
///     Статус репоста сообщения.
/// </summary>
public enum RepostStatus
{
	/// <summary>
	///     Ожидает репоста.
	/// </summary>
	Pending = 0,

	/// <summary>
	///     Успешно репостнуто.
	/// </summary>
	Success = 1,

	/// <summary>
	///     Ошибка при репосте.
	/// </summary>
	Failed = 2
}
