namespace Shared.Exceptions;

/// <summary>
///     Исключение, выбрасываемое когда требуется повторная авторизация в Telegram (например, при 401 ошибке).
/// </summary>
public sealed class TelegramReauthorizationRequiredException : SharedException
{
	public Guid SessionId { get; }

	public TelegramReauthorizationRequiredException(Guid sessionId, Exception? innerException = null)
		: base($"Требуется повторная авторизация для сессии {sessionId}")
	{
		SessionId = sessionId;
		if (innerException != null)
		{
			InnerException = innerException;
		}
	}

	public new Exception? InnerException { get; init; }
}
