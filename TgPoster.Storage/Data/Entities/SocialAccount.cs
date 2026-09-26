using Shared.Enums;

namespace TgPoster.Storage.Data.Entities;

/// <summary>
///     Подключённый аккаунт соцсети пользователя
/// </summary>
public sealed class SocialAccount : BaseEntity
{
	/// <summary>
	///     Id пользователя
	/// </summary>
	public required Guid UserId { get; set; }

	/// <summary>
	///     Площадка
	/// </summary>
	public SocialPlatform Platform { get; set; }

	/// <summary>
	///     Handle/username для показа
	/// </summary>
	public required string Name { get; set; }

	/// <summary>
	///     Идентификатор пользователя на площадке (для Bluesky — DID)
	/// </summary>
	public required string ExternalUserId { get; set; }

	/// <summary>
	///     Зашифрованный секрет аккаунта
	/// </summary>
	public required string Secret { get; set; }

	/// <summary>
	///     Срок действия токена, null для Bluesky
	/// </summary>
	public DateTimeOffset? TokenExpiresAt { get; set; }

	/// <summary>
	///     Статус аккаунта
	/// </summary>
	public SocialAccountStatus Status { get; set; } = SocialAccountStatus.Active;

	/// <summary>
	///     Последняя ошибка авторизации или публикации
	/// </summary>
	public string? LastError { get; set; }

	#region Навигация

	/// <summary>
	///     Пользователь
	/// </summary>
	public User User { get; set; } = null!;

	/// <summary>
	///     Связки расписаний с этим аккаунтом
	/// </summary>
	public ICollection<CrossPostTarget> CrossPostTargets { get; set; } = [];

	#endregion
}
