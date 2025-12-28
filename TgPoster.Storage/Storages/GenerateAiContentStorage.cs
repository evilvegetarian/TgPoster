using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.Messages.GenerateAiContent;
using TgPoster.API.Domain.UseCases.Messages.ListMessage;
using TgPoster.Storage.Data;

namespace TgPoster.Storage.Storages;

public class GenerateAiContentStorage(PosterContext context) : IGenerateAiContentStorage
{
	public Task<OpenRouterDto?> GetOpenRouterAsync(Guid messagId, CancellationToken ct)
	{
		return context.Messages
			.Where(m => m.Id == messagId && m.Schedule.OpenRouterSetting != null)
			.Select(x => new OpenRouterDto
			{
				Model = x.Schedule.OpenRouterSetting!.Model,
				Token = x.Schedule.OpenRouterSetting.TokenHash,
				ScheduleId = x.Schedule.Id
			})
			.FirstOrDefaultAsync(ct);
	}

	public Task<GenerateAiContentPromptSettingDto?> GetPromptSettingsAsync(Guid scheduleId, CancellationToken ct)
	{
		return context.PromptSettings
			.Where(x => x.ScheduleId == scheduleId)
			.Select(x => new GenerateAiContentPromptSettingDto
			{
				TextPrompt = x.TextPrompt,
				PhotoPrompt = x.TextPrompt,
				VideoPrompt = x.TextPrompt
			})
			.FirstOrDefaultAsync(ct);
	}

	public Task<GenerateAiContentMessageDto?> GetMessageAsync(Guid messageId, CancellationToken ct)
	{
		return context.Messages
			.Where(m => m.Id == messageId)
			.Select(x => new GenerateAiContentMessageDto
			{
				Id = x.Id,
				TextMessage = x.TextMessage,
				Files = x.MessageFiles.Select(file => new FileDto
					{
						Id = file.Id,
						ContentType = file.ContentType,
						TgFileId = file.TgFileId,
						Previews = file.Thumbnails.Select(thumbnail => new PreviewDto
						{
							Id = thumbnail.Id,
							TgFileId = thumbnail.TgFileId
						}).ToList()
					})
					.ToList()
			})
			.FirstOrDefaultAsync(ct);
	}
}