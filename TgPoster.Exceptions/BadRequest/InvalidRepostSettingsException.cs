using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.BadRequest;

public sealed class InvalidRepostSettingsException(string message) : DomainException(message);