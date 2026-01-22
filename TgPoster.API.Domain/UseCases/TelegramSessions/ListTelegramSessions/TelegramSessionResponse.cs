namespace TgPoster.API.Domain.UseCases.TelegramSessions.ListTelegramSessions;

public sealed record TelegramSessionResponse(
	Guid Id,
	string PhoneNumber,
	string? Name,
	bool IsActive,
	DateTimeOffset? Created
);