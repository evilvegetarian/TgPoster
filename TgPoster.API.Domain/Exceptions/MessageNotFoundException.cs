namespace TgPoster.API.Domain.Exceptions;

public class MessageNotFoundException(Guid id) : NotFoundException($"Message with id {id} not found");