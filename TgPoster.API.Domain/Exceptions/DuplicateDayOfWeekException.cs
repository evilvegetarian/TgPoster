namespace TgPoster.API.Domain.Exceptions;

/// <summary>
///     Исключение, выбрасываемое при попытке добавить дубликат дня недели в расписание.
/// </summary>
public sealed class DuplicateDayOfWeekException(DayOfWeek dayOfWeek)
	: DomainException($"День недели {dayOfWeek} уже существует в расписании");
