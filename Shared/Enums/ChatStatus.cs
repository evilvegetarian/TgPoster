namespace Shared.Enums;

/// <summary>
///     Статус доступа к чату в Telegram.
/// </summary>
public enum ChatStatus
{
	/// <summary>
	///     Активен, доступ есть.
	/// </summary>
	Active = 0,

	/// <summary>
	///     Забанен в чате.
	/// </summary>
	Banned = 1,

	/// <summary>
	///     Покинул чат.
	/// </summary>
	Left = 2,

	/// <summary>
	///     Статус неизвестен.
	/// </summary>
	Unknown = 3
}
