using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.PromptSetting.GetPromptSetting;
using TgPoster.API.Domain.UseCases.PromptSetting.ListPromptSetting;
using TgPoster.Storage.Data;

namespace TgPoster.Storage.Storages;

public class GetPromptSettingStorage(PosterContext context) : IGetPromptSettingStorage
{
	public Task<PromptSettingResponse?> GetAsync(
		Guid id,
		Guid userId,
		CancellationToken cancellationToken
	)
	{
		return context.PromptSettings
			.Where(x => x.Schedule.UserId == userId)
			.Select(x => new PromptSettingResponse
			{
				Id = x.Id,
				VideoPrompt = x.VideoPrompt,
				PicturePrompt = x.PicturePrompt,
				TextPrompt = x.TextPrompt,
			}).FirstOrDefaultAsync(cancellationToken);
	}
}