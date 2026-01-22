namespace Shared.Exceptions;

/// <summary>
///     Исключение, выбрасываемое когда нет активной сессии авторизации для завершения процесса.
/// </summary>
public sealed class TelegramAuthSessionNotFoundException(Guid sessionId)
	: SharedException($"Нет активной сессии авторизации для {sessionId}");
