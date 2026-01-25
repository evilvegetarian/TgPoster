using Microsoft.EntityFrameworkCore;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.Enum;
using TgPoster.Worker.Domain.UseCases.RepostMessageConsumer;

namespace TgPoster.Storage.Storages.Repost;

internal sealed class RepostMessageConsumerStorage(PosterContext context) : IRepostMessageConsumerStorage
{
	public Task<RepostDataDto?> GetRepostDataAsync(Guid messageId, CancellationToken ct)
	{
		return context.Messages
			.Where(m => m.Id == messageId)
			.Select(m => m.Schedule.RepostSettings != null && m.Schedule.RepostSettings.IsActive
				? new RepostDataDto
				{
					TelegramMessageId = m.TelegramMessageId,
					TelegramSessionId = m.Schedule.RepostSettings.TelegramSessionId,
					SourceChannelIdentifier = m.Schedule.ChannelName,
					Destinations = m.Schedule.RepostSettings.Destinations
						.Where(d => d.IsActive)
						.Select(d => new RepostDestinationDataDto
						{
							Id = d.Id,
							ChatIdentifier = d.ChatId
						}).ToList()
				}
				: null)
			.FirstOrDefaultAsync(ct);
	}

	public async Task CreateRepostLogAsync(
		Guid messageId,
		Guid repostDestinationId,
		int? telegramMessageId,
		string? error,
		CancellationToken ct)
	{
		var log = new RepostLog
		{
			Id = Guid.NewGuid(),
			MessageId = messageId,
			RepostDestinationId = repostDestinationId,
			TelegramMessageId = telegramMessageId,
			Status = error == null ? RepostStatus.Success : RepostStatus.Failed,
			RepostedAt = error == null ? DateTime.UtcNow : null,
			Error = error
		};

		context.Set<RepostLog>().Add(log);
		await context.SaveChangesAsync(ct);
	}
}
