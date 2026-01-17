namespace Shared.Exceptions;

public class OpenRouterNotFoundException(Guid id) : SharedException($"Не найдены эти настройки OpenRouter {id}");