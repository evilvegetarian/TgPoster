namespace Shared.Enums;

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
	Failed = 2,

	/// <summary>
	///     Репост осознанно пропущен по настройкам рандомизации или лимитам.
	/// </summary>
	Skipped = 3
}
