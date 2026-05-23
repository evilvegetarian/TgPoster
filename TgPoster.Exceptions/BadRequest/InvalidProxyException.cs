namespace TgPoster.Exceptions;

public sealed class InvalidProxyException(string message) : DomainException(message);
