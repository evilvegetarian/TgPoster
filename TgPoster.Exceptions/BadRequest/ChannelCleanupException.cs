using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.BadRequest;

/// <summary>
/// Очистку каналов нельзя построить или запустить
/// </summary>
/// <param name="message">Причина для пользователя</param>
public sealed class ChannelCleanupException(string message) : DomainException(message);
