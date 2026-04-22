namespace Shared.Exceptions;

/// <summary>
///     Исключение, выбрасываемое когда данные Telegram сессии повреждены и WTelegram не может их распарсить.
///     Сессия деактивирована и требует повторной авторизации.
/// </summary>
public sealed class TelegramSessionCorruptedException(Guid sessionId, Exception? innerException = null)
    : SharedException($"Данные Telegram сессии {sessionId} повреждены. Сессия деактивирована, требуется повторная авторизация.")
{
    public Guid SessionId { get; } = sessionId;

    public new Exception? InnerException { get; } = innerException;
}
