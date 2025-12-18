namespace Shared.SharedException;

public class OpenRouterException(string? message = null) : SharedException($"Ошибка Open Router. {message}");