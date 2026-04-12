namespace Shared.Exceptions;

/// <summary>
///     Исключение, выбрасываемое когда Telegram отклоняет ключ авторизации сессии (AUTH_KEY_DUPLICATED).
///     Сессия необратимо инвалидирована и требует полной повторной авторизации.
/// </summary>
public sealed class TelegramAuthKeyDuplicatedException(Guid sessionId, Exception? innerException = null)
    : SharedException($"Ключ авторизации дублирован для сессии {sessionId}. Сессия деактивирована.")
{
    public Guid SessionId { get; } = sessionId;

    public new Exception? InnerException { get; } = innerException;
}
