using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.Schedules.UpdateSchedule;
using TgPoster.Storage.Data;

namespace TgPoster.Storage.Storages;

internal sealed class UpdateScheduleStorage(PosterContext context) : IUpdateScheduleStorage
{
	public async Task UpdateScheduleAsync(Guid id, Guid userId, string? name, Guid? youTubeAccountId, Guid? telegramBotId, CancellationToken ct)
	{
		var schedule = await context.Schedules
			.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);

		if (schedule is null)
		{
			return;
		}

		if (name is not null)
			schedule.Name = name;

		if (telegramBotId is not null)
			schedule.TelegramBotId = telegramBotId.Value;

		schedule.YouTubeAccountId = youTubeAccountId;
		await context.SaveChangesAsync(ct);
	}

	public Task<string?> GetApiTokenAsync(Guid telegramBotId, Guid userId, CancellationToken ct)
	{
		return context.TelegramBots.Where(x => x.Id == telegramBotId)
			.Where(x => x.OwnerId == userId)
			.Select(x => x.ApiTelegram)
			.FirstOrDefaultAsync(ct);
	}

	public Task<string?> GetChannelNameAsync(Guid scheduleId, Guid userId, CancellationToken ct)
	{
		return context.Schedules.Where(x => x.Id == scheduleId && x.UserId == userId)
			.Select(x => x.ChannelName)
			.FirstOrDefaultAsync(ct);
	}
}
