using Shared.Enums;

namespace TgPoster.Storage.Data.Entities;

/// <summary>
///     Задание на массовое добавление целевых каналов из Discover.
///     Каналы обрабатываются фоново по одному, чтобы не ловить FLOOD_WAIT на аккаунт.
/// </summary>
public sealed class RepostImportJob : BaseEntity
{
	/// <summary>
	///     Id настроек репоста, в которые добавляются каналы.
	/// </summary>
	public required Guid RepostSettingsId { get; set; }

	/// <summary>
	///     Настройки репоста.
	/// </summary>
	public RepostSettings RepostSettings { get; set; } = null!;

	/// <summary>
	///     Вступать ли автоматически в приватные каналы по invite-ссылке.
	/// </summary>
	public bool AutoJoin { get; set; }

	/// <summary>
	///     Текущий статус задания.
	/// </summary>
	public RepostImportStatus Status { get; set; }

	/// <summary>
	///     Момент начала обработки.
	/// </summary>
	public DateTimeOffset? StartedAt { get; set; }

	/// <summary>
	///     Момент завершения обработки.
	/// </summary>
	public DateTimeOffset? CompletedAt { get; set; }

	/// <summary>
	///     Причина остановки задания.
	/// </summary>
	public string? LastError { get; set; }

	/// <summary>
	///     Каналы задания.
	/// </summary>
	public ICollection<RepostImportJobItem> Items { get; set; } = [];
}
