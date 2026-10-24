using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.Messages.DeletePublishedMessage;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Enum;

namespace TgPoster.Storage.Storages;

internal sealed class DeletePublishedMessageStorage(PosterContext context) : IDeletePublishedMessageStorage
{
	public Task<PublishedMessageInfo?> GetMessageAsync(Guid id, Guid userId, CancellationToken ct)
	{
		return context.Messages
			.Where(x => x.Id == id)
			.Where(x => x.Schedule.UserId == userId)
			.Select(x => new PublishedMessageInfo
			{
				ScheduleId = x.ScheduleId,
				ChannelId = x.Schedule.ChannelId,
				TimePosting = x.TimePosting,
				IsPublished = x.Status == MessageStatus.Send,
				TelegramMessageIds = x.TelegramMessageIds
			})
			.FirstOrDefaultAsync(ct);
	}

	public async Task DeleteMessageAsync(Guid id, CancellationToken ct)
	{
		var message = await context.Messages.FirstAsync(x => x.Id == id, ct);
		context.Messages.Remove(message);
		await context.SaveChangesAsync(ct);
	}
}
