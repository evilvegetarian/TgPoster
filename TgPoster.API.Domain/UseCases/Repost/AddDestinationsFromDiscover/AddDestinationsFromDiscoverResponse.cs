using System.ComponentModel.DataAnnotations;

namespace TgPoster.API.Domain.UseCases.Repost.AddDestinationsFromDiscover;

/// <summary>
///     Итог обработки одного канала при массовом добавлении
/// </summary>
public enum AddDestinationOutcome
{
	/// <summary>
	///     Канал добавлен в список целевых
	/// </summary>
	Added = 0,

	/// <summary>
	///     Канал уже был добавлен ранее
	/// </summary>
	AlreadyAdded = 1,

	/// <summary>
	///     Это канал-источник расписания, репостить сам в себя нельзя
	/// </summary>
	SourceChannel = 2,

	/// <summary>
	///     Нет прав на отправку сообщений
	/// </summary>
	NoWritePermission = 3,

	/// <summary>
	///     Нет прав на отправку медиа
	/// </summary>
	NoMediaPermission = 4,

	/// <summary>
	///     Канал не удалось найти или вступить в него
	/// </summary>
	NotResolved = 5,

	/// <summary>
	///     Telegram ограничил аккаунт, канал не обработан — можно повторить позже
	/// </summary>
	RateLimited = 6
}

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
///     Результат массового добавления целевых каналов из Discover
/// </summary>
public sealed record AddDestinationsFromDiscoverResponse
{
	/// <summary>
	///     Результат по каждому запрошенному каналу
	/// </summary>
	[Required]
	public required List<AddDestinationResultDto> Results { get; init; }

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
	///     Прервана ли обработка из-за ограничений Telegram
	/// </summary>
	[Required]
	public required bool RateLimited { get; init; }
}
