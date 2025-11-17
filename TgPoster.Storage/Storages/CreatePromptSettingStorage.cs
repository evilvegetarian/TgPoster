using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.PromptSetting.CreatePromptSetting;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Storages;

internal class CreatePromptSettingStorage(PosterContext context, GuidFactory guidFactory) : ICreatePromptSettingStorage
{
	public Task<bool> ExistScheduleAsync(Guid scheduleId, Guid userId, CancellationToken ctx)
	{
		return context.Schedules.AnyAsync(x => x.Id == scheduleId && x.UserId == userId, ctx);
	}

	public async Task<Guid> CreatePromptSettingAsync(
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
		return entity.Id;
	}
}