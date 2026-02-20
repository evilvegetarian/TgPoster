using Shared.Enums;

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
	///     ID целевого канала/чата/группы.
	/// </summary>
	public required long ChatId { get; set; }

	/// <summary>
	///     Активен ли целевой канал.
	/// </summary>
	public bool IsActive { get; set; } = true;

	/// <summary>
	///     Название канала/чата.
	/// </summary>
	public string? Title { get; set; }

	/// <summary>
	///     Username (без @).
	/// </summary>
	public string? Username { get; set; }

	/// <summary>
	///     Количество подписчиков/участников.
	/// </summary>
	public int? MemberCount { get; set; }

	/// <summary>
	///     Тип чата (канал/группа).
	/// </summary>
	public ChatType ChatType { get; set; } = ChatType.Unknown;

	/// <summary>
	///     Статус доступа к чату (активен/забанен/покинул).
	/// </summary>
	public ChatStatus ChatStatus { get; set; } = ChatStatus.Unknown;

	/// <summary>
	///     Аватарка в формате base64 data URI.
	/// </summary>
	public string? AvatarBase64 { get; set; }

	/// <summary>
	///     Дата последнего обновления информации.
	/// </summary>
	public DateTimeOffset? InfoUpdatedAt { get; set; }

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
