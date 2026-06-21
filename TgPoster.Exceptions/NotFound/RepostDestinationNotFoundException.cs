using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.NotFound;

public sealed class RepostDestinationNotFoundException(Guid id)
	: NotFoundException($"Целевой канал с ID {id} не найден");