namespace Shared.Enums;

/// <summary>
///     Куда вести ссылку в кросс-посте
/// </summary>
public enum CrossPostLinkTarget
{
	/// <summary>
	///     Ссылка на конкретный пост в Telegram
	/// </summary>
	Post = 1,

	/// <summary>
	///     Ссылка на канал Telegram
	/// </summary>
	Channel = 2,

	/// <summary>
	///     Произвольная ссылка
	/// </summary>
	Custom = 3,

	/// <summary>
	///     Без ссылки
	/// </summary>
	None = 4
}
