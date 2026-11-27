namespace TgPoster.API.Domain.UseCases.TelegramSessions.ImportTelegramSession;

public interface IImportTelegramSessionStorage
{
	Task<TelegramApiCredentials?> GetApiCredentialsAsync(Guid userId, Guid sessionId, CancellationToken ct);

	Task<Guid> CreateAsync(
		Guid userId,
		string apiId,
		string apiHash,
		string phoneNumber,
		string? name,
		string sessionData,
		Guid? notificationBotId,
		CancellationToken ct
	);
}
