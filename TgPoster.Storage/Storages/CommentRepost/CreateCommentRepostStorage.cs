using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.CommentRepost.CreateCommentRepost;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Storages.CommentRepost;

internal sealed class CreateCommentRepostStorage(PosterContext context, GuidFactory guidFactory)
	: ICreateCommentRepostStorage
{
	public Task<bool> ScheduleExistsAsync(Guid scheduleId, Guid userId, CancellationToken ct)
	{
		return context.Schedules.AnyAsync(x => x.Id == scheduleId && x.UserId == userId, ct);
	}

	public Task<bool> TelegramSessionExistsAndActiveAsync(Guid telegramSessionId, CancellationToken ct)
	{
		return context.TelegramSessions.AnyAsync(x => x.Id == telegramSessionId && x.IsActive, ct);
	}

	public Task<bool> SettingsExistsAsync(long watchedChannelId, Guid scheduleId, CancellationToken ct)
	{
		return context.CommentRepostSettings.AnyAsync(
			x => x.WatchedChannelId == watchedChannelId && x.ScheduleId == scheduleId, ct);
	}

	public async Task<Guid> CreateAsync(
		string watchedChannel,
		long watchedChannelId,
		long? watchedChannelAccessHash,
		long discussionGroupId,
		long? discussionGroupAccessHash,
		Guid telegramSessionId,
		Guid scheduleId,
		CancellationToken ct)
	{
		var id = guidFactory.New();
		var settings = new CommentRepostSettings
		{
			Id = id,
			WatchedChannel = watchedChannel,
			WatchedChannelId = watchedChannelId,
			WatchedChannelAccessHash = watchedChannelAccessHash,
			DiscussionGroupId = discussionGroupId,
			DiscussionGroupAccessHash = discussionGroupAccessHash,
			TelegramSessionId = telegramSessionId,
			ScheduleId = scheduleId,
			IsActive = true
		};

		await context.AddAsync(settings, ct);
		await context.SaveChangesAsync(ct);

		return id;
	}
}
