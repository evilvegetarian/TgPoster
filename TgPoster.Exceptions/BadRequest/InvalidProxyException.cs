using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.BadRequest;

public sealed class InvalidProxyException(string message) : DomainException(message);
