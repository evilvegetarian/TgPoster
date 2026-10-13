using Shared.Enums;

namespace TgPoster.Storage.Data.Entities;

/// <summary>
/// Задание на выход аккаунта из каналов и групп, которые приложение не использует.
/// Выходит по одному каналу с паузами, чтобы не поймать FLOOD_WAIT
/// </summary>
internal sealed class ChannelCleanupJob : BaseEntity
{
	/// <summary>
	/// Id Telegram-сессии, чей аккаунт выходит из каналов
	/// </summary>
	public required Guid TelegramSessionId { get; set; }

	/// <summary>
	/// Telegram-сессия
	/// </summary>
	public TelegramSession TelegramSession { get; set; } = null!;

	/// <summary>
	/// Текущий статус задания
	/// </summary>
	public ChannelCleanupStatus Status { get; set; }

	/// <summary>
	/// Момент начала последнего прохода обработки
	/// </summary>
	public DateTimeOffset? StartedAt { get; set; }

	/// <summary>
	/// Момент завершения обработки
	/// </summary>
	public DateTimeOffset? CompletedAt { get; set; }

	/// <summary>
	/// Причина паузы или остановки задания
	/// </summary>
	public string? LastError { get; set; }

	/// <summary>
	/// Каналы задания
	/// </summary>
	public ICollection<ChannelCleanupJobItem> Items { get; set; } = [];
}
