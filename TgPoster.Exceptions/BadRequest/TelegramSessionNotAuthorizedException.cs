namespace TgPoster.Exceptions;

/// <summary>
///     Исключение, выбрасываемое когда Telegram сессия не авторизована (нет данных сессии)
/// </summary>
/// <param name="sessionId">Идентификатор Telegram-сессии</param>
public sealed class TelegramSessionNotAuthorizedException(Guid sessionId)
	: DomainException($"Telegram сессия {sessionId} не авторизована. Необходимо завершить авторизацию.");
