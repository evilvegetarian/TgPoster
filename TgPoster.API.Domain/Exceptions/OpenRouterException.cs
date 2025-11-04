namespace TgPoster.API.Domain.Exceptions;

public class OpenRouterException(string? message = null) : DomainException($"Ошибка Open Router. {message}");