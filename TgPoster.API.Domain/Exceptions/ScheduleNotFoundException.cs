namespace TgPoster.API.Domain.Exceptions;

public class ScheduleNotFoundException(Guid id) : NotFoundException($"Расписание с ID {id} не найдено");