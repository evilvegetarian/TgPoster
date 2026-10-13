using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.ChannelCleanup.PreviewChannelCleanup;
using TgPoster.Storage.Data;
using TgPoster.Telegram.Models;

namespace TgPoster.Storage.Storages.ChannelCleanup;

internal sealed class PreviewChannelCleanupStorage(PosterContext context) : IPreviewChannelCleanupStorage
{
	public Task<bool> SessionExistsAsync(Guid sessionId, Guid userId, CancellationToken ct)
	{
		return context.TelegramSessions.AnyAsync(x => x.Id == sessionId && x.UserId == userId, ct);
	}

	public Task<UsedTelegramChats> GetUsedChatsAsync(Guid sessionId, CancellationToken ct)
	{
		return UsedTelegramChatsQuery.LoadAsync(context, sessionId, ct);
	}
}
