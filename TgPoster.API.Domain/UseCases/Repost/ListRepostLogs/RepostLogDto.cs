using Shared.Enums;

namespace TgPoster.API.Domain.UseCases.Repost.ListRepostLogs;

/// <summary>
///     Запись журнала репостов: что и куда репостили и чем это закончилось.
/// </summary>
public sealed record RepostLogDto
{
	/// <summary>
	///     Id записи журнала.
	/// </summary>
	public required Guid Id { get; init; }

	/// <summary>
	///     Дата и время записи.
	/// </summary>
	public required DateTimeOffset CreatedAt { get; init; }

	/// <summary>
	///     Id настроек репоста.
	/// </summary>
	public required Guid RepostSettingsId { get; init; }

	/// <summary>
	///     Название расписания, к которому привязаны настройки репоста.
	/// </summary>
	public required string ScheduleName { get; init; }

	/// <summary>
	///     Канал-источник, из которого делался репост.
	/// </summary>
	public required string SourceChannelName { get; init; }

	/// <summary>
	///     Id репостнутого сообщения.
	/// </summary>
	public required Guid MessageId { get; init; }

	/// <summary>
	///     Начало текста сообщения для быстрого опознания поста.
	/// </summary>
	public string? MessagePreview { get; init; }

	/// <summary>
	///     Время публикации сообщения в канале-источнике.
	/// </summary>
	public required DateTimeOffset MessageTimePosting { get; init; }

	/// <summary>
	///     Id целевого канала в системе.
	/// </summary>
	public required Guid DestinationId { get; init; }

	/// <summary>
	///     Id целевого чата в Telegram.
	/// </summary>
	public required long DestinationChatId { get; init; }

	/// <summary>
	///     Название целевого канала.
	/// </summary>
	public string? DestinationTitle { get; init; }

	/// <summary>
	///     Username целевого канала (без @).
	/// </summary>
	public string? DestinationUsername { get; init; }

	/// <summary>
	///     Чем закончилась попытка репоста.
	/// </summary>
	public required RepostStatus Status { get; init; }

	/// <summary>
	///     Детальная причина статуса.
	/// </summary>
	public required RepostLogReason Reason { get; init; }

	/// <summary>
	///     Id пересланного сообщения в целевом канале.
	/// </summary>
	public int? TelegramMessageId { get; init; }

	/// <summary>
	///     Текст ошибки или пояснение к пропуску.
	/// </summary>
	public string? Error { get; init; }

	/// <summary>
	///     Дата и время успешного репоста.
	/// </summary>
	public DateTime? RepostedAt { get; init; }
}
