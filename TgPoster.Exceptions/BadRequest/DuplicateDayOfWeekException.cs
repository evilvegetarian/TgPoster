namespace TgPoster.Exceptions;

/// <summary>
///     Исключение, выбрасываемое при попытке добавить дубликат дня недели в расписание
/// </summary>
/// <param name="dayOfWeek">Дублирующийся день недели</param>
public sealed class DuplicateDayOfWeekException(DayOfWeek dayOfWeek)
	: DomainException($"День недели {dayOfWeek} уже существует в расписании");
