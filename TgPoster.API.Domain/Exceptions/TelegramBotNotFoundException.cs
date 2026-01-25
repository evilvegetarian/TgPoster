namespace TgPoster.API.Domain.Exceptions;

public class TelegramBotNotFoundException(Guid? id = null) : NotFoundException($"Telegram бот с ID {id} не найден.");