using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.BadRequest;

/// <summary>
///     Исключение, выбрасываемое когда Telegram сессия неактивна
/// </summary>
/// <param name="sessionId">Идентификатор Telegram-сессии</param>
public sealed class TelegramSessionInactiveException(Guid sessionId)
	: DomainException($"Telegram сессия {sessionId} неактивна");