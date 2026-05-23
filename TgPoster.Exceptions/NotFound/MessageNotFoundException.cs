namespace TgPoster.Exceptions;

public class MessageNotFoundException(Guid id) : NotFoundException($"Сообщение с ID {id} не найдено");
