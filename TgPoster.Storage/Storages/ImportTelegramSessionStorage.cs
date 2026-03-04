using TgPoster.API.Domain.UseCases.TelegramSessions.ImportTelegramSession;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.Enum;

namespace TgPoster.Storage.Storages;

internal sealed class ImportTelegramSessionStorage(PosterContext context, GuidFactory guidFactory)
	: IImportTelegramSessionStorage
{
	public async Task<Guid> CreateAsync(
		Guid userId,
		string apiId,
		string apiHash,
		string phoneNumber,
		string? name,
		string sessionData,
		CancellationToken ct
	)
	{
		var session = new TelegramSession
		{
			Id = guidFactory.New(),
			ApiId = apiId,
			ApiHash = apiHash,
			PhoneNumber = phoneNumber,
			Name = name,
			UserId = userId,
			IsActive = true,
			Status = TelegramSessionStatus.Authorized,
			SessionData = sessionData
		};

		await context.TelegramSessions.AddAsync(session, ct);
		await context.SaveChangesAsync(ct);

		return session.Id;
	}
}
