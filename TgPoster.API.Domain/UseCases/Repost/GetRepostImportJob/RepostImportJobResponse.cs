using System.ComponentModel.DataAnnotations;
using Shared.Enums;

namespace TgPoster.API.Domain.UseCases.Repost.GetRepostImportJob;

/// <summary>
///     Результат обработки одного канала из Discover
/// </summary>
public sealed record AddDestinationResultDto
{
	/// <summary>
	///     Id записи в Discover
	/// </summary>
	[Required]
	public required Guid DiscoveredChannelId { get; init; }

	/// <summary>
	///     Название канала для отображения в интерфейсе
	/// </summary>
	[Required]
	public required string Title { get; init; }

	/// <summary>
	///     Итог обработки канала
	/// </summary>
	[Required]
	public required AddDestinationOutcome Outcome { get; init; }

	/// <summary>
	///     Id созданного целевого канала. Заполнен только при Outcome = Added
	/// </summary>
	public Guid? DestinationId { get; init; }

	/// <summary>
	///     Причина отказа, если канал не был добавлен
	/// </summary>
	public string? Error { get; init; }
}

/// <summary>
///     Состояние задания на массовое добавление целевых каналов из Discover
/// </summary>
public sealed record RepostImportJobResponse
{
	/// <summary>
	///     Id задания, по нему опрашивается прогресс
	/// </summary>
	[Required]
	public required Guid JobId { get; init; }

	/// <summary>
	///     Текущий статус задания
	/// </summary>
	[Required]
	public required RepostImportStatus Status { get; init; }

	/// <summary>
	///     Всего каналов в задании
	/// </summary>
	[Required]
	public required int TotalCount { get; init; }

	/// <summary>
	///     Сколько каналов добавлено
	/// </summary>
	[Required]
	public required int AddedCount { get; init; }

	/// <summary>
	///     Сколько каналов пропущено (уже добавлены, источник, нет прав, не найдены)
	/// </summary>
	[Required]
	public required int SkippedCount { get; init; }

	/// <summary>
	///     Сколько каналов ещё ждёт фоновой обработки
	/// </summary>
	[Required]
	public required int PendingCount { get; init; }

	/// <summary>
	///     Через сколько секунд задание продолжится. Заполнено, когда Telegram ограничил сессию
	/// </summary>
	public int? RetryAfterSeconds { get; init; }

	/// <summary>
	///     Результат по каждому запрошенному каналу
	/// </summary>
	[Required]
	public required List<AddDestinationResultDto> Results { get; init; }
}
