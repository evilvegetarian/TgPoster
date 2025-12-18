using Shared.SharedException;

namespace TgPoster.API.Domain.Exceptions;

public class OpenRouterException(string? message = null) : SharedException($"Ошибка Open Router. {message}");