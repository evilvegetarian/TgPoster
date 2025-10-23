namespace TgPoster.API.Models;

/// <summary>
///     Запрос на получение списка сообщений с фильтрацией, сортировкой и пагинацией.
/// </summary>
public class ListMessagesRequest : PaginationRequest
{
	/// <summary>
	///     ID расписания, к которому относятся сообщения. Обязательный параметр.
	/// </summary>
	public required Guid ScheduleId { get; set; }

	/// <summary>
	///     Фильтр по статусу сообщения.
	/// </summary>
	public MessageStatus Status { get; set; } = MessageStatus.All;

	/// <summary>
	///     Текстовый поиск по содержимому сообщения.
	/// </summary>
	public string? SearchText { get; set; }

	/// <summary>
	///     Начальная дата создания для фильтрации (включительно).
	/// </summary>
	public DateTime? CreatedFrom { get; set; }

	/// <summary>
	///     Конечная дата создания для фильтрации (включительно).
	/// </summary>
	public DateTime? CreatedTo { get; set; }

	/// <summary>
	///     Поле для сортировки. По умолчанию - по дате создания.
	/// </summary>
	public MessageSortBy SortBy { get; set; } = MessageSortBy.SentAt;

	/// <summary>
	///     Направление сортировки. По умолчанию - по убыванию (сначала новые).
	/// </summary>
	public SortDirection SortDirection { get; set; } = SortDirection.Asc;
}