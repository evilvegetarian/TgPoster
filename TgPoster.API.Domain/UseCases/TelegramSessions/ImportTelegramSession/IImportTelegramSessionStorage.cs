namespace TgPoster.API.Domain.UseCases.TelegramSessions.ImportTelegramSession;

public interface IImportTelegramSessionStorage
{
	Task<Guid> CreateAsync(
		Guid userId,
		string apiId,
		string apiHash,
		string phoneNumber,
		string? name,
		string sessionData,
		CancellationToken ct
	);
}
