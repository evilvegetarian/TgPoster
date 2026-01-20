namespace TgPoster.API.Models;

public sealed record CreateTelegramSessionRequest(
	string ApiId,
	string ApiHash,
	string PhoneNumber,
	string? Name
);
