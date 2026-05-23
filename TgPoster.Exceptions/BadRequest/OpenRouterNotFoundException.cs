using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.BadRequest;

public class OpenRouterNotFoundException(Guid id) : DomainException($"Не найдены эти настройки OpenRouter {id}");
