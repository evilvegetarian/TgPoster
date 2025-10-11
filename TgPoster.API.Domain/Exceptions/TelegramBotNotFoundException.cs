namespace TgPoster.API.Domain.Exceptions;

public class TelegramBotNotFoundException(Guid? id = null) : NotFoundException($"TelegramBot {id} does not exist.");