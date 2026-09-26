using Shared.Enums;

namespace TgPoster.Storage.Data.Entities;

/// <summary>
///     Публикация одного поста в один аккаунт соцсети
/// </summary>
public sealed class CrossPost : BaseEntity
{
	/// <summary>
	///     Id сообщения
	/// </summary>
	public required Guid MessageId { get; set; }

	/// <summary>
	///     Id связки расписание-аккаунт
	/// </summary>
	public required Guid CrossPostTargetId { get; set; }

	/// <summary>
	///     Id аккаунта соцсети, денормализация
	/// </summary>
	public required Guid SocialAccountId { get; set; }

	/// <summary>
	///     Площадка, денормализация
	/// </summary>
	public SocialPlatform Platform { get; set; }

	/// <summary>
	///     Имя аккаунта для показа после удаления аккаунта
	/// </summary>
	public required string AccountName { get; set; }

	/// <summary>
	///     Статус публикации
	/// </summary>
	public CrossPostStatus Status { get; set; }

	/// <summary>
	///     Когда публиковать
	/// </summary>
	public DateTimeOffset ScheduledAt { get; set; }

	/// <summary>
	///     Начало текущей попытки
	/// </summary>
	public DateTimeOffset? StartedAt { get; set; }

	/// <summary>
	///     Количество попыток
	/// </summary>
	public int Attempts { get; set; }

	/// <summary>
	///     Когда опубликовано
	/// </summary>
	public DateTimeOffset? PublishedAt { get; set; }

	/// <summary>
	///     Внешний id поста в соцсети
	/// </summary>
	public string? ExternalPostId { get; set; }

	/// <summary>
	///     Ссылка на пост в соцсети
	/// </summary>
	public string? ExternalUrl { get; set; }

	/// <summary>
	///     Ошибка или причина пропуска
	/// </summary>
	public string? Error { get; set; }

	#region Навигация

	/// <summary>
	///     Сообщение
	/// </summary>
	public Message Message { get; set; } = null!;

	/// <summary>
	///     Связка расписание-аккаунт
	/// </summary>
	public CrossPostTarget CrossPostTarget { get; set; } = null!;

	/// <summary>
	///     Аккаунт соцсети
	/// </summary>
	public SocialAccount SocialAccount { get; set; } = null!;

	#endregion
}
