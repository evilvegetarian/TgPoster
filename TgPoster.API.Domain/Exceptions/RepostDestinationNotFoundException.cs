namespace TgPoster.API.Domain.Exceptions;

public sealed class RepostDestinationNotFoundException(Guid id)
	: NotFoundException($"Целевой канал с ID {id} не найден");
