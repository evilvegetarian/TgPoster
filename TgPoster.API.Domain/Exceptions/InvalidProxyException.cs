namespace TgPoster.API.Domain.Exceptions;

public sealed class InvalidProxyException(string message) : DomainException(message);
