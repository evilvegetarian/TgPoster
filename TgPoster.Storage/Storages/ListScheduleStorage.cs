using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.Schedules.ListSchedule;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Enum;

namespace TgPoster.Storage.Storages;

internal sealed class ListScheduleStorage(PosterContext context) : IListScheduleStorage
{
	public Task<List<ScheduleResponse>> GetListScheduleAsync(Guid userId, CancellationToken ct)
	{
		return context.Schedules.Where(x => x.UserId == userId)
			.Select(x => new ScheduleResponse
			{
				Id = x.Id,
				Name = x.Name,
				ChannelName = x.ChannelName,
				IsActive = x.IsActive,
				BotName = x.TelegramBot.Name,
				TelegramBotId = x.TelegramBotId,
				OpenRouterId = x.OpenRouterSetting != null ? x.OpenRouterSetting.Id : null,
				PromptId = x.PromptSetting != null ? x.PromptSetting.Id : null,
				YouTubeAccountId = x.YouTubeAccountId,
				PostCount = x.Messages.Count,
				PendingPostCount = x.Messages.Count(m => m.Status == MessageStatus.Register&& m.TimePosting>DateTime.Now),
				LastPostDate = x.Messages.Max(m => (DateTimeOffset?)m.TimePosting)
			}).ToListAsync(ct);
	}
}