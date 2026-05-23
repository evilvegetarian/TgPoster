namespace TgPoster.Exceptions;

public class OpenRouterException(string? message = null) : DomainException($"Ошибка Open Router. {message}");
