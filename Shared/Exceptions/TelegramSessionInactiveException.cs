namespace Shared.Exceptions;

/// <summary>
///     Исключение, выбрасываемое когда Telegram сессия неактивна.
/// </summary>
public sealed class TelegramSessionInactiveException(Guid sessionId)
	: SharedException($"Telegram сессия {sessionId} неактивна");