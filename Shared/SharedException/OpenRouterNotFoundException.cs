namespace Shared.SharedException;

public class OpenRouterNotFoundException(Guid id) : SharedException($"Не найдены эти настройки OpenRouter {id}");