namespace TgPoster.API.Domain.UseCases.TelegramSessions.ImportTelegramSession;

/// <summary>
///     Ответ на импорт Telegram сессии
/// </summary>
public sealed record ImportTelegramSessionResponse(
	Guid Id,
	string Name,
	bool IsActive,
	string PhoneNumber
);
