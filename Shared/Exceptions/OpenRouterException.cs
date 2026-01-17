namespace Shared.Exceptions;

public class OpenRouterException(string? message = null) : SharedException($"Ошибка Open Router. {message}");