using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.TelegramBots.DeleteTelegramBot;
using TgPoster.Storage.Data;

namespace TgPoster.Storage.Storages;

public class DeleteTelegramBotStorage(PosterContext context) : IDeleteTelegramBotStorage
{
	public Task<bool> ExistsAsync(Guid id, Guid currentUserId, CancellationToken ct)
	{
		return context.TelegramBots.AnyAsync(x => x.Id == id && x.OwnerId == currentUserId, ct);
	}

	public Task DeleteTelegramBotAsync(Guid id, Guid userId, CancellationToken ct)
	{
		return context.TelegramBots
			.Where(x => x.Id == id && x.OwnerId == userId)
			.ExecuteUpdateAsync(b => b.SetProperty(x => x.Deleted, DateTime.UtcNow), ct);
	}
}