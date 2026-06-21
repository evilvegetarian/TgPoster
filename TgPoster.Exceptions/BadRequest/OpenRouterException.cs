using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.BadRequest;

public class OpenRouterException(string? message = null) : DomainException($"Ошибка Open Router. {message}");