using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.BadRequest;

/// <summary>
///     Исключение, выбрасываемое когда Telegram сессия не найдена по ID на стороне runtime-клиента
/// </summary>
/// <param name="sessionId">Идентификатор Telegram-сессии</param>
public sealed class TelegramSessionNotFoundException(Guid sessionId)
	: DomainException($"Telegram сессия с ID {sessionId} не найдена");
