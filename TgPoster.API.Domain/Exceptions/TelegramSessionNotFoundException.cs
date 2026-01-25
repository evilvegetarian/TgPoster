namespace TgPoster.API.Domain.Exceptions;

public sealed class TelegramSessionNotFoundException(Guid? id = null)
	: NotFoundException($"Telegram сессия с ID {id} не найдена.");