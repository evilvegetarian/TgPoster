namespace Shared.Exceptions;

/// <summary>
///     Исключение, выбрасываемое когда Telegram сессия не найдена по ID.
/// </summary>
public sealed class TelegramSessionNotFoundException(Guid sessionId)
	: SharedException($"Telegram сессия с ID {sessionId} не найдена");