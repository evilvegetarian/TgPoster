namespace TgPoster.API.Domain.UseCases.TelegramSessions.CreateTelegramSession;

public interface ICreateTelegramSessionStorage
{
	Task<CreateTelegramSessionResponse> CreateAsync(
		Guid userId,
		string apiId,
		string apiHash,
		string phoneNumber,
		string? name,
		CancellationToken ct
	);
}
