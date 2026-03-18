using Microsoft.EntityFrameworkCore;
using TgPoster.Storage.Data;
using TgPoster.Worker.Domain.UseCases.ClassifyChannel;

namespace TgPoster.Storage.Storages.ClassifyChannel;

internal sealed class ClassifyChannelStorage(PosterContext context) : IClassifyChannelStorage
{
	public Task<ChannelForClassificationDto?> GetChannelForClassificationAsync(Guid id, CancellationToken ct)
	{
		return context.DiscoveredChannels
			.Where(x => x.Id == id)
			.Select(x => new ChannelForClassificationDto
			{
				Id = x.Id,
				Title = x.Title,
				Description = x.Description
			})
			.FirstOrDefaultAsync(ct);
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
		channel.ClassifiedAt = DateTime.UtcNow;

		await context.SaveChangesAsync(ct);
	}
}
