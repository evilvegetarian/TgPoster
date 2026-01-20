using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.TelegramSessions.DeleteTelegramSession;
using TgPoster.Storage.Data;

namespace TgPoster.Storage.Storages;

internal sealed class DeleteTelegramSessionStorage(PosterContext context) : IDeleteTelegramSessionStorage
{
	public Task<bool> ExistsAsync(Guid userId, Guid sessionId, CancellationToken ct)
	{
		return context.TelegramSessions
			.AnyAsync(s => s.Id == sessionId && s.UserId == userId , ct);
	}

	public async Task DeleteAsync(Guid sessionId, CancellationToken ct)
	{
		var session = await context.TelegramSessions
			.FirstAsync(s => s.Id == sessionId, ct);

		context.TelegramSessions.Remove(session);
		await context.SaveChangesAsync(ct);
	}
}
