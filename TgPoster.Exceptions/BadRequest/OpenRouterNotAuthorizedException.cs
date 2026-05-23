using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.BadRequest;

public class OpenRouterNotAuthorizedException() : DomainException("Токен не валиден");
