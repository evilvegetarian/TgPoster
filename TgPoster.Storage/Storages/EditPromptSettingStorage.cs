using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.PromptSetting.EditPromptSetting;
using TgPoster.Storage.Data;

namespace TgPoster.Storage.Storages;

public class EditPromptSettingStorage(PosterContext context) : IEditPromptSettingStorage
{
	public Task<bool> ExistPromptAsync(Guid id, Guid userId, CancellationToken ctx)
	{
		return context.PromptSettings.AnyAsync(x => x.Id == id && x.Schedule.UserId == userId, ctx);
	}

	public async Task UpdatePromptAsync(
		Guid id,
		string? text,
		string? video,
		string? photo,
		CancellationToken ctx
	)
	{
		var entity = await context.PromptSettings.FirstOrDefaultAsync(x => x.Id == id, ctx);
		entity!.PicturePrompt = photo;
		entity.VideoPrompt = video;
		entity.TextPrompt = text;
		await context.SaveChangesAsync(ctx);
	}
}