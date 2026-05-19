using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using TgPoster.Storage.Data;
using TgPoster.Worker.Domain.UseCases.ClassifyChannel;

namespace TgPoster.Storage.Storages.ClassifyChannel;

internal sealed class ClassifyChannelStorage(PosterContext context) : IClassifyChannelStorage
{
	public Task<Guid?> GetSessionIdByPurposeAsync(TelegramSessionPurpose purpose, CancellationToken ct)
		=> context.TelegramSessions
			.Where(s => s.IsActive
			            && s.Purposes.Contains(purpose))
			.Select(s => (Guid?)s.Id)
			.FirstOrDefaultAsync(ct);

	public Task<List<ChannelForClassificationDto>> GetUnclassifiedChannelsAsync(int batchSize, CancellationToken ct)
	{
		//Пока это только ищу
		
		var query = context.DiscoveredChannels
			.Where(x => x.PeerType == "chat")
			.Where(x => x.Category == "18+");
		
		return query
			.Where(x => x.Username != null)
			.OrderBy(x => x.LastClassifiedAt != null)
			.Take(batchSize)
			.Select(x => new ChannelForClassificationDto
			{
				Id = x.Id,
				Title = x.Title,
				Description = x.Description,
				Username = x.Username,
				TelegramId = x.TelegramId
			})
			.ToListAsync(ct);
	}

	public async Task UpdateClassificationAsync(
		Guid id,
		string? category,
		string? subcategory,
		string[]? tags,
		string? language,
		double? confidence,
		CancellationToken ct
	)
	{
		var channel = await context.DiscoveredChannels.FirstAsync(x => x.Id == id, ct);

		channel.Category = category;
		channel.Subcategory = subcategory;
		channel.Tags = tags;
		channel.Language = language;
		channel.ClassificationConfidence = confidence;
		channel.LastClassifiedAt = DateTimeOffset.UtcNow;

		await context.SaveChangesAsync(ct);
	}

	public async Task MarkChannelBannedAsync(Guid channelId, CancellationToken ct)
	{
		var entity = await context.DiscoveredChannels.Where(x => x.Id == channelId).FirstOrDefaultAsync(ct);
		if (entity is not null)
		{
			entity.IsBanned = true;
		}

		await context.SaveChangesAsync(ct);
	}
}