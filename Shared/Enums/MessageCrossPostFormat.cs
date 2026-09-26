namespace Shared.Enums;

/// <summary>
///     Формат кросс-поста для отдельного сообщения, наследуемый из настроек связки
/// </summary>
public enum MessageCrossPostFormat
{
	/// <summary>
	///     Использовать формат из настроек связки
	/// </summary>
	Inherit = 0,

	/// <summary>
	///     Короткий тизер
	/// </summary>
	Teaser = 1,

	/// <summary>
	///     Полный текст поста
	/// </summary>
	Full = 2,

	/// <summary>
	///     Анонс с призывом
	/// </summary>
	Announcement = 3
}
