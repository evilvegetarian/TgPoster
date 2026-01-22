using TgPoster.API.Domain.UseCases.TelegramSessions.CreateTelegramSession;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Storages;

internal sealed class CreateTelegramSessionStorage(PosterContext context, GuidFactory guidFactory)
	: ICreateTelegramSessionStorage
{
	public async Task<CreateTelegramSessionResponse> CreateAsync(
		Guid userId,
		string apiId,
		string apiHash,
		string phoneNumber,
		string? name,
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
			IsActive = true
		};

		await context.TelegramSessions.AddAsync(session, ct);
		await context.SaveChangesAsync(ct);

		return new CreateTelegramSessionResponse(session.Id, session.Name ?? phoneNumber, session.IsActive,
			string.Empty);
	}
}