using Microsoft.EntityFrameworkCore;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.Enum;
using TgPoster.Worker.Domain.UseCases.RepostMessageConsumer;

namespace TgPoster.Storage.Storages.Repost;

internal sealed class RepostMessageConsumerStorage(PosterContext context) : IRepostMessageConsumerStorage
{
	public Task<RepostDataDto?> GetRepostDataAsync(Guid messageId, Guid repostSettingsId, CancellationToken ct)
	{
		return context.Set<RepostSettings>()
			.Where(rs => rs.Id == repostSettingsId && rs.IsActive)
			.Select(rs => new RepostDataDto
			{
				TelegramMessageId = rs.Schedule.Messages
					.Where(m => m.Id == messageId)
					.Select(m => m.TelegramMessageId)
					.FirstOrDefault(),
				TelegramSessionId = rs.TelegramSessionId,
				SourceChannelIdentifier = rs.Schedule.ChannelName,
				Destinations = rs.Destinations
					.Where(d => d.IsActive)
					.Select(d => new RepostDestinationDataDto
					{
						Id = d.Id,
						ChatIdentifier = d.ChatId
					}).ToList()
			})
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
