namespace TgPoster.API.Domain.Exceptions;

public class OpenRouterNotAuthorizedException() : DomainException("Токен не валиден");

public class OpenRouterNotFoundException(Guid id) : NotFoundException($"Не найдены эти настройки OpenRouter {id}");