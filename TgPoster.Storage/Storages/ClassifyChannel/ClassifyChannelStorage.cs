using Microsoft.EntityFrameworkCore;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Worker.Domain.UseCases.ClassifyChannel;

namespace TgPoster.Storage.Storages.ClassifyChannel;

internal sealed class ClassifyChannelStorage(PosterContext context) : IClassifyChannelStorage
{
	public Task<ClassifierSettingsDto?> GetSettingsAsync(CancellationToken ct) =>
		context.ClassifierSettings
			.Where(x => x.Id == ClassifierSettings.SingletonId)
			.Select(x => new ClassifierSettingsDto
			{
				IsEnabled = x.IsEnabled,
				Model = x.Model,
				BatchSize = x.BatchSize,
				IntervalMinutes = x.IntervalMinutes,
				MessageSampleCount = x.MessageSampleCount,
				PhotoCount = x.PhotoCount,
				ReclassifyAfterDays = x.ReclassifyAfterDays,
				Categories = x.Categories,
				SystemPrompt = x.SystemPrompt
			})
			.FirstOrDefaultAsync(ct);

	public async Task EnsureSettingsAsync(ClassifierSettingsDto defaults, CancellationToken ct)
	{
		var exists = await context.ClassifierSettings
			.IgnoreQueryFilters()
			.AnyAsync(x => x.Id == ClassifierSettings.SingletonId, ct);
		if (exists)
		{
			return;
		}

		context.ClassifierSettings.Add(new ClassifierSettings
		{
			Id = ClassifierSettings.SingletonId,
			IsEnabled = defaults.IsEnabled,
			Model = defaults.Model,
			BatchSize = defaults.BatchSize,
			IntervalMinutes = defaults.IntervalMinutes,
			MessageSampleCount = defaults.MessageSampleCount,
			PhotoCount = defaults.PhotoCount,
			ReclassifyAfterDays = defaults.ReclassifyAfterDays,
			Categories = [..defaults.Categories],
			SystemPrompt = defaults.SystemPrompt
		});
		await context.SaveChangesAsync(ct);
	}

	public Task<List<ChannelForClassificationDto>> GetChannelsToClassifyAsync(
		int batchSize,
		DateTimeOffset retryBefore,
		DateTimeOffset? reclassifyBefore,
		CancellationToken ct
	)
	{
		// Postgres при ORDER BY ASC ставит NULL в конец, поэтому «ни разу не пробовали» поднимаем отдельным ключом
		return context.DiscoveredChannels
			.Where(x => x.Username != null)
			.Where(x => x.LastClassifiedAt == null
			            || (reclassifyBefore != null && x.LastClassifiedAt < reclassifyBefore))
			.Where(x => x.LastClassificationAttemptAt == null || x.LastClassificationAttemptAt < retryBefore)
			.OrderBy(x => x.LastClassifiedAt != null)
			.ThenBy(x => x.LastClassificationAttemptAt != null)
			.ThenBy(x => x.LastClassificationAttemptAt)
			.ThenBy(x => x.Id)
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

	public async Task MarkClassificationAttemptAsync(Guid id, DateTimeOffset attemptedAt, CancellationToken ct)
	{
		var channel = await context.DiscoveredChannels.FirstAsync(x => x.Id == id, ct);
		channel.LastClassificationAttemptAt = attemptedAt;
		await context.SaveChangesAsync(ct);
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
