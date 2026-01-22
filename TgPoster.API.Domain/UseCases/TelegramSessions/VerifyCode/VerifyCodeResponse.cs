namespace TgPoster.API.Domain.UseCases.TelegramSessions.VerifyCode;

public sealed record VerifyCodeResponse(
	bool Success,
	bool RequiresPassword,
	string? Message
);