using Shared.SharedException;

namespace TgPoster.API.Domain.Exceptions;

public class OpenRouterNotAuthorizedException() : SharedException("Токен не валиден");