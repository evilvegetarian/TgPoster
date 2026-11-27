namespace TgPoster.API.Domain.UseCases.TelegramSessions.CreateTelegramSession;

public interface ICreateTelegramSessionStorage
{
	Task<TelegramApiCredentials?> GetApiCredentialsAsync(Guid userId, Guid sessionId, CancellationToken ct);

	Task<CreateTelegramSessionResponse> CreateAsync(
		Guid userId,
		string apiId,
		string apiHash,
		string phoneNumber,
		string? name,
		Guid? proxyId,
		Guid? notificationBotId,
		CancellationToken ct
	);
}
