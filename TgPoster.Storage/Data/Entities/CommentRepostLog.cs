using TgPoster.Storage.Data.Enum;

namespace TgPoster.Storage.Data.Entities;

/// <summary>
///     Журнал отправленных комментариев к постам внешних каналов.
/// </summary>
public sealed class CommentRepostLog : BaseEntity
{
	/// <summary>
	///     ID настроек комментирующего репоста.
	/// </summary>
	public required Guid CommentRepostSettingsId { get; set; }

	/// <summary>
	///     ID оригинального поста во внешнем канале.
	/// </summary>
	public required int OriginalPostId { get; set; }

	/// <summary>
	///     ID пересланного сообщения (наш пост) в Telegram.
	/// </summary>
	public int? ForwardedMessageId { get; set; }

	/// <summary>
	///     ID комментария в discussion group.
	/// </summary>
	public int? CommentMessageId { get; set; }

	/// <summary>
	///     Статус отправки.
	/// </summary>
	public RepostStatus Status { get; set; }

	/// <summary>
	///     Текст ошибки.
	/// </summary>
	public string? Error { get; set; }

	/// <summary>
	///     Дата отправки.
	/// </summary>
	public DateTime? SentAt { get; set; }

	#region Navigation Properties

	/// <summary>
	///     Настройки комментирующего репоста.
	/// </summary>
	public CommentRepostSettings CommentRepostSettings { get; set; } = null!;

	#endregion
}
