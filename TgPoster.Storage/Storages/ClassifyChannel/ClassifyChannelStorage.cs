using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Enum;
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
		return context.DiscoveredChannels
			.Where(x => x.LastClassifiedAt == null && (x.Title != null || x.Description != null))
			.OrderBy(x => x.Id)
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
		CancellationToken ct)
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
}
