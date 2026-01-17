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

	public async Task DeleteTelegramBotAsync(Guid id, Guid userId, CancellationToken ct)
	{
		var bot = await context.TelegramBots
			.Where(x => x.Id == id && x.OwnerId == userId)
			.FirstOrDefaultAsync(ct);
		context.TelegramBots.Remove(bot!);
		await context.SaveChangesAsync(ct);
	}
}