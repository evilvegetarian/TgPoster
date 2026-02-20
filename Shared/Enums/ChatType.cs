namespace Shared.Enums;

/// <summary>
///     Тип чата в Telegram.
/// </summary>
public enum ChatType
{
	/// <summary>
	///     Канал.
	/// </summary>
	Channel = 0,

	/// <summary>
	///     Группа/супергруппа.
	/// </summary>
	Group = 1,

	/// <summary>
	///     Неизвестный тип.
	/// </summary>
	Unknown = 2
}
