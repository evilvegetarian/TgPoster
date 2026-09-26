using Shared.Enums;

namespace TgPoster.Storage.Data.Entities;

/// <summary>
///     Связка расписания с аккаунтом соцсети и её настройки
/// </summary>
public sealed class CrossPostTarget : BaseEntity
{
	/// <summary>
	///     Id расписания
	/// </summary>
	public required Guid ScheduleId { get; set; }

	/// <summary>
	///     Id аккаунта соцсети
	/// </summary>
	public required Guid SocialAccountId { get; set; }

	/// <summary>
	///     Активна ли связка
	/// </summary>
	public bool IsActive { get; set; } = true;

	/// <summary>
	///     Формат кросс-поста по умолчанию
	/// </summary>
	public CrossPostFormat Format { get; set; }

	/// <summary>
	///     Куда вести ссылку
	/// </summary>
	public CrossPostLinkTarget LinkTarget { get; set; }

	/// <summary>
	///     Произвольная ссылка, обязательна при LinkTarget == Custom
	/// </summary>
	public string? CustomLink { get; set; }

	/// <summary>
	///     Варианты призыва построчно
	/// </summary>
	public string? CallToAction { get; set; }

	/// <summary>
	///     Прикладывать медиа к кросс-посту
	/// </summary>
	public bool IncludeMedia { get; set; } = true;

	/// <summary>
	///     Кросс-постить посты, созданные парсером чужих каналов
	/// </summary>
	public bool IncludeParsed { get; set; }

	/// <summary>
	///     Задержка публикации в минутах
	/// </summary>
	public int DelayMinutes { get; set; }

	#region Навигация

	/// <summary>
	///     Расписание
	/// </summary>
	public Schedule Schedule { get; set; } = null!;

	/// <summary>
	///     Аккаунт соцсети
	/// </summary>
	public SocialAccount SocialAccount { get; set; } = null!;

	/// <summary>
	///     Кросс-посты по этой связке
	/// </summary>
	public ICollection<CrossPost> CrossPosts { get; set; } = [];

	#endregion
}
