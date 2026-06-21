using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.NotFound;

public class ChatIdNotFoundException() : NotFoundException("Напишите сообщение в боте для идентификации личности.");