using Shared.Enums;

namespace TgPoster.Storage.Data.Entities;

/// <summary>
/// Один канал или группа внутри задания на очистку
/// </summary>
internal sealed class ChannelCleanupJobItem : BaseEntity
{
	/// <summary>
	/// Id задания
	/// </summary>
	public required Guid ChannelCleanupJobId { get; set; }

	/// <summary>
	/// Задание, которому принадлежит канал
	/// </summary>
	public ChannelCleanupJob ChannelCleanupJob { get; set; } = null!;

	/// <summary>
	/// Сырой Telegram-id канала или группы (без префикса -100)
	/// </summary>
	public required long ChatId { get; set; }

	/// <summary>
	/// Access hash канала для этого аккаунта. У обычной группы 0
	/// </summary>
	public long AccessHash { get; set; }

	/// <summary>
	/// Обычная группа: из неё выходят другим методом и без access hash
	/// </summary>
	public bool IsBasicGroup { get; set; }

	/// <summary>
	/// Тип чата для отображения
	/// </summary>
	public ChatType ChatType { get; set; }

	/// <summary>
	/// Название на момент постановки в очередь
	/// </summary>
	public required string Title { get; set; }

	/// <summary>
	/// Username без @ на момент постановки в очередь
	/// </summary>
	public string? Username { get; set; }

	/// <summary>
	/// Порядковый номер: задаёт и порядок обработки, и порядок вывода
	/// </summary>
	public int Order { get; set; }

	/// <summary>
	/// Итог обработки
	/// </summary>
	public ChannelCleanupOutcome Outcome { get; set; }

	/// <summary>
	/// Причина, если выйти не удалось
	/// </summary>
	public string? Error { get; set; }

	/// <summary>
	/// Момент обработки
	/// </summary>
	public DateTimeOffset? ProcessedAt { get; set; }
}
