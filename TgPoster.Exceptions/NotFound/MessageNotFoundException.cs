using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.NotFound;

public class MessageNotFoundException(Guid id) : NotFoundException($"Сообщение с ID {id} не найдено");