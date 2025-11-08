using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.PromptSetting.CreatePromptSetting;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Storages;

internal class CreatePromptSettingStorage(PosterContext context, GuidFactory guidFactory) : ICreatePromptSettingStorage
{
	public Task<bool> ExistScheduleAsync(Guid scheduleId, CancellationToken ctx)
	{
		return context.Schedules.AnyAsync(x => x.Id == scheduleId, ctx);
	}

	public async Task CreatePromptSettingAsync(
		Guid scheduleId,
		string? textPrompt,
		string? videoPrompt,
		string? photoPrompt,
		CancellationToken ctx
	)
	{
		var entity = new PromptSetting
		{
			Id = guidFactory.New(),
			ScheduleId = scheduleId,
			PicturePrompt = photoPrompt,
			VideoPrompt = videoPrompt,
			TextPrompt = textPrompt
		};
		await context.PromptSettings.AddAsync(entity, ctx);
		await context.SaveChangesAsync(ctx);
	}
}