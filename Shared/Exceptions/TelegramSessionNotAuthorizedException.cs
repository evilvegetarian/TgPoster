namespace Shared.Exceptions;

/// <summary>
///     Исключение, выбрасываемое когда Telegram сессия не авторизована (нет данных сессии).
/// </summary>
public sealed class TelegramSessionNotAuthorizedException(Guid sessionId)
	: SharedException($"Telegram сессия {sessionId} не авторизована. Необходимо завершить авторизацию.");