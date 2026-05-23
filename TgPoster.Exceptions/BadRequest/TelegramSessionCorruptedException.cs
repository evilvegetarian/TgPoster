namespace TgPoster.Exceptions;

/// <summary>
///     Исключение, выбрасываемое когда данные Telegram сессии повреждены и WTelegram не может их распарсить.
///     Сессия деактивирована и требует повторной авторизации
/// </summary>
/// <param name="sessionId">Идентификатор Telegram-сессии</param>
/// <param name="innerException">Исходное исключение парсинга</param>
public sealed class TelegramSessionCorruptedException(Guid sessionId, Exception? innerException = null)
    : DomainException($"Данные Telegram сессии {sessionId} повреждены. Сессия деактивирована, требуется повторная авторизация.")
{
    public Guid SessionId { get; } = sessionId;

    public new Exception? InnerException { get; } = innerException;
}
