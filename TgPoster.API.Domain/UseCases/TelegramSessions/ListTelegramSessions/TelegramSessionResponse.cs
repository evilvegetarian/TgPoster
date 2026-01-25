using Shared.Telegram;

namespace TgPoster.API.Domain.UseCases.TelegramSessions.ListTelegramSessions;

public sealed record TelegramSessionResponse(
	Guid Id,
	string PhoneNumber,
	string? Name,
	bool IsActive,
	TelegramSessionStatus Status,
	DateTimeOffset? Created
);

public sealed record TelegramSessionListResponse
{
	public required List<TelegramSessionResponse> Items { get; init; }
}