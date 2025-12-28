using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.OpenRouterSetting.AddScheduleOpenRouterSetting;
using TgPoster.Storage.Data;

namespace TgPoster.Storage.Storages;

public class AddScheduleOpenRouterSettingStorage(PosterContext context) : IAddScheduleOpenRouterSettingStorage
{
	public Task<bool> ExistScheduleAsync(Guid scheduleId, Guid userId, CancellationToken ctx)
	{
		return context.Schedules.AnyAsync(x => x.Id == scheduleId && x.UserId == userId, ctx);
	}

	public Task<bool> ExistOpenRouterAsync(Guid openRouterId, Guid userId, CancellationToken ctx)
	{
		return context.OpenRouterSettings.AnyAsync(x => x.Id == openRouterId && x.UserId == userId, ctx);
	}

	public async Task UpdateOpenRouterAsync(Guid openRouterId, Guid scheduleId, CancellationToken ctx)
	{
		var entity = await context.OpenRouterSettings.FirstOrDefaultAsync(ctx);
		entity!.ScheduleId = scheduleId;
		await context.SaveChangesAsync(ctx);
	}
}