using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.TelegramSessions;
using TgPoster.API.Domain.UseCases.TelegramSessions.CreateTelegramSession;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Storages;

internal sealed class CreateTelegramSessionStorage(PosterContext context, GuidFactory guidFactory)
	: ICreateTelegramSessionStorage
{
	public Task<TelegramApiCredentials?> GetApiCredentialsAsync(Guid userId, Guid sessionId, CancellationToken ct) =>
		context.TelegramSessions
			.Where(s => s.Id == sessionId && s.UserId == userId)
			.Select(s => new TelegramApiCredentials(s.ApiId, s.ApiHash))
			.FirstOrDefaultAsync(ct);

	public async Task<CreateTelegramSessionResponse> CreateAsync(
		Guid userId,
		string apiId,
		string apiHash,
		string phoneNumber,
		string? name,
		Guid? proxyId,
		Guid? notificationBotId,
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
			ProxyId = proxyId,
			NotificationBotId = notificationBotId
		};

		await context.TelegramSessions.AddAsync(session, ct);
		await context.SaveChangesAsync(ct);

		return new CreateTelegramSessionResponse(session.Id, session.Name ?? phoneNumber, session.IsActive,
			string.Empty);
	}
}
