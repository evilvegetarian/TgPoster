namespace TgPoster.Exceptions;

/// <summary>
///     Исключение, выбрасываемое когда нет активной сессии авторизации для завершения процесса
/// </summary>
/// <param name="sessionId">Идентификатор Telegram-сессии</param>
public sealed class TelegramAuthSessionNotFoundException(Guid sessionId)
	: DomainException($"Нет активной сессии авторизации для {sessionId}");
