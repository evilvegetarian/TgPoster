using TgPoster.Storage.Data.Enum;

namespace TgPoster.Storage.Data.Entities;

/// <summary>
///     Журнал выполненных репостов.
/// </summary>
public sealed class RepostLog : BaseEntity
{
	/// <summary>
	///     ID сообщения, которое было репостнуто.
	/// </summary>
	public required Guid MessageId { get; set; }

	/// <summary>
	///     Целевой канал, в который был сделан репост.
	/// </summary>
	public required Guid RepostDestinationId { get; set; }

	/// <summary>
	///     Статус репоста.
	/// </summary>
	public RepostStatus Status { get; set; }

	/// <summary>
	///     ID сообщения в Telegram целевого канала.
	/// </summary>
	public int? TelegramMessageId { get; set; }

	/// <summary>
	///     Дата и время выполнения репоста.
	/// </summary>
	public DateTime? RepostedAt { get; set; }

	/// <summary>
	///     Текст ошибки, если репост не удался.
	/// </summary>
	public string? Error { get; set; }

	#region Navigation Properties

	/// <summary>
	///     Сообщение, которое было репостнуто.
	/// </summary>
	public Message Message { get; set; } = null!;

	/// <summary>
	///     Целевой канал репоста.
	/// </summary>
	public RepostDestination RepostDestination { get; set; } = null!;

	#endregion
}
