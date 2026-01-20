using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.TelegramSessions.StartAuth;
using TgPoster.Storage.Data;

namespace TgPoster.Storage.Storages;

internal sealed class StartAuthStorage(PosterContext context) : IStartAuthStorage
{
	public Task<bool> ExistsAsync(Guid userId, Guid sessionId, CancellationToken ct)
	{
		return context.TelegramSessions
			.AnyAsync(s => s.Id == sessionId && s.UserId == userId, ct);
	}
}
