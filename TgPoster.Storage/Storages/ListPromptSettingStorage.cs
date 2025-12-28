using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.PromptSetting.ListPromptSetting;
using TgPoster.Storage.Data;

namespace TgPoster.Storage.Storages;

public class ListPromptSettingStorage(PosterContext context) : IListPromptSettingStorage
{
	public Task<List<PromptSettingResponse>> GetAsync(Guid userId, CancellationToken cancellationToken)
	{
		return context.PromptSettings
			.Where(x => x.Schedule.UserId == userId)
			.Select(x => new PromptSettingResponse
			{
				Id = x.Id,
				VideoPrompt = x.VideoPrompt,
				PicturePrompt = x.PicturePrompt,
				TextPrompt = x.TextPrompt
			}).ToListAsync(cancellationToken);
	}
}