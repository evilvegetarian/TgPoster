using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.NotFound;

public class ScheduleNotFoundException(Guid id) : NotFoundException($"Расписание с ID {id} не найдено");