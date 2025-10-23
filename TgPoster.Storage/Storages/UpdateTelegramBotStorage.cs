using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.TelegramBots.UpdateTelegramBot;
using TgPoster.Storage.Data;

namespace TgPoster.Storage.Storages;

public class UpdateTelegramBotStorage(PosterContext context) : IUpdateTelegramBotStorage
{
	public async Task UpdateTelegramBotAsync(Guid id, string name, bool isActive, CancellationToken ct)
	{
		var telegramBot = await context.TelegramBots.FirstOrDefaultAsync(x => x.Id == id, ct);
		telegramBot!.Name = name;
		await context.SaveChangesAsync(ct);
	}
}