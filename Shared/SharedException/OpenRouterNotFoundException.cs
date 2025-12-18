using Shared.SharedException;

namespace TgPoster.API.Domain.Exceptions;

public class OpenRouterNotFoundException(Guid id) : SharedException($"Не найдены эти настройки OpenRouter {id}");