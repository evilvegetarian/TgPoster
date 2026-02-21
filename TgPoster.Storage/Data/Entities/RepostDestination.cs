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

	/// <summary>
	///     Минимальная задержка перед репостом (секунды).
	/// </summary>
	public int DelayMinSeconds { get; set; }

	/// <summary>
	///     Максимальная задержка перед репостом (секунды).
	/// </summary>
	public int DelayMaxSeconds { get; set; }

	/// <summary>
	///     Репостить каждое N-е сообщение (1 = каждое).
	/// </summary>
	public int RepostEveryNth { get; set; } = 1;

	/// <summary>
	///     Вероятность пропуска репоста (0-100%).
	/// </summary>
	public int SkipProbability { get; set; }

	/// <summary>
	///     Максимальное количество репостов в день (null = без лимита).
	/// </summary>
	public int? MaxRepostsPerDay { get; set; }

	/// <summary>
	///     Счётчик сообщений для RepostEveryNth.
	/// </summary>
	public int RepostCounter { get; set; }

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
