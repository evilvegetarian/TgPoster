namespace TgPoster.Exceptions;

public sealed class InvalidRepostSettingsException(string message) : DomainException(message);
