namespace TgPoster.API.Domain.Exceptions;

public class ScheduleNotFoundException(Guid id) : NotFoundException($"Schedule with id {id} not found");