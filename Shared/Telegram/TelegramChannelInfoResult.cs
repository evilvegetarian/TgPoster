namespace Shared.Telegram;

/// <summary>
///     Расширенная информация о Telegram канале/чате.
/// </summary>
public sealed class TelegramChannelInfoResult
{
	/// <summary>
	///     Название чата/канала.
	/// </summary>
	public required string Title { get; init; }

	/// <summary>
	///     Username (если публичный).
	/// </summary>
	public string? Username { get; init; }

	/// <summary>
	///     Количество подписчиков/участников.
	/// </summary>
	public int? MemberCount { get; init; }

	/// <summary>
	///     Это канал (не группа).
	/// </summary>
	public bool IsChannel { get; init; }

	/// <summary>
	///     Это группа/супергруппа.
	/// </summary>
	public bool IsGroup { get; init; }

	/// <summary>
	///     Миниатюра аватарки (маленькая фото).
	/// </summary>
	public byte[]? AvatarThumbnail { get; init; }
}
