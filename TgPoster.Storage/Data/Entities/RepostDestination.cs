namespace TgPoster.Storage.Data.Entities;

/// <summary>
///     Целевой канал/чат для репоста.
/// </summary>
public sealed class RepostDestination : BaseEntity
{
	/// <summary>
	///     Настройки репоста, к которым привязан этот целевой канал.
	/// </summary>
	public required Guid RepostSettingsId { get; set; }

	/// <summary>
	///     ID или @username целевого канала/чата/группы.
	/// </summary>
	public required long ChatId { get; set; }

	/// <summary>
	///     Активен ли целевой канал.
	/// </summary>
	public bool IsActive { get; set; } = true;

	#region Navigation Properties

	/// <summary>
	///     Настройки репоста.
	/// </summary>
	public RepostSettings RepostSettings { get; set; } = null!;

	/// <summary>
	///     Журналы репостов в этот канал.
	/// </summary>
	public ICollection<RepostLog> RepostLogs { get; set; } = [];

	#endregion
}
