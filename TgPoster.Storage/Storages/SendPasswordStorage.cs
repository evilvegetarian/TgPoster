using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.TelegramSessions.SendPassword;
using TgPoster.Storage.Data;

namespace TgPoster.Storage.Storages;

internal sealed class SendPasswordStorage(PosterContext context) : ISendPasswordStorage
{
	public Task<bool> ExistsAsync(Guid userId, Guid sessionId, CancellationToken ct)
	{
		return context.TelegramSessions
			.AnyAsync(s => s.Id == sessionId && s.UserId == userId && s.Deleted == null, ct);
	}
}
