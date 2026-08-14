using Shared.Enums;

namespace TgPoster.Storage.Data.Entities;

/// <summary>
///     Один канал внутри задания на массовое добавление целевых каналов.
/// </summary>
public sealed class RepostImportJobItem : BaseEntity
{
	/// <summary>
	///     Id задания.
	/// </summary>
	public required Guid RepostImportJobId { get; set; }

	/// <summary>
	///     Задание, которому принадлежит канал.
	/// </summary>
	public RepostImportJob RepostImportJob { get; set; } = null!;

	/// <summary>
	///     Id записи в Discover. Без внешнего ключа: в задание попадают и запрошенные id,
	///     которых в Discover уже нет.
	/// </summary>
	public required Guid DiscoveredChannelId { get; set; }

	/// <summary>
	///     Название канала на момент постановки в очередь (для отображения в интерфейсе).
	/// </summary>
	public required string Title { get; set; }

	/// <summary>
	///     Порядковый номер канала в задании: задаёт и порядок обработки, и порядок вывода.
	/// </summary>
	public int Order { get; set; }

	/// <summary>
	///     Итог обработки канала.
	/// </summary>
	public AddDestinationOutcome Outcome { get; set; }

	/// <summary>
	///     Id созданного целевого канала. Заполнен только при Outcome = Added.
	/// </summary>
	public Guid? RepostDestinationId { get; set; }

	/// <summary>
	///     Причина отказа, если канал не был добавлен.
	/// </summary>
	public string? Error { get; set; }

	/// <summary>
	///     Момент, когда канал был обработан.
	/// </summary>
	public DateTimeOffset? ProcessedAt { get; set; }
}
