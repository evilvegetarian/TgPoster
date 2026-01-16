namespace TgPoster.API.Models;

/// <summary>
/// Фильтр для списка сообщений
/// </summary>
public class ListMessageFilter
{
	/// <summary>
	/// Идентификатор расписания для фильтрации
	/// </summary>
	public required Guid ScheduleId { get; set; }
}