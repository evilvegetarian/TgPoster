using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.NotFound;

public class TelegramBotNotFoundException(Guid? id = null) : NotFoundException($"Telegram бот с ID {id} не найден.");
