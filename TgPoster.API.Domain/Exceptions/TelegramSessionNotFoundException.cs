namespace TgPoster.API.Domain.Exceptions;

public sealed class TelegramSessionNotFoundException(Guid? id = null)
	: NotFoundException($"TelegramSession {id} does not exist.");
