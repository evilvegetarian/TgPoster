namespace TgPoster.Exceptions;

public class OpenRouterNotFoundException(Guid id) : DomainException($"Не найдены эти настройки OpenRouter {id}");
