using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.Discover.GetClassifierSettings;
using TgPoster.API.Domain.UseCases.Discover.UpdateClassifierSettings;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Storages;

internal sealed class ClassifierSettingsStorage(PosterContext context)
	: IGetClassifierSettingsStorage, IUpdateClassifierSettingsStorage
{
	public Task<SavedClassifierSettingsDto?> GetClassifierSettingsAsync(CancellationToken ct) =>
		context.ClassifierSettings
			.Where(x => x.Id == ClassifierSettings.SingletonId)
			.Select(x => new SavedClassifierSettingsDto
			{
				IsEnabled = x.IsEnabled,
				Model = x.Model,
				BatchSize = x.BatchSize,
				IntervalMinutes = x.IntervalMinutes,
				MessageSampleCount = x.MessageSampleCount,
				PhotoCount = x.PhotoCount,
				ReclassifyAfterDays = x.ReclassifyAfterDays,
				Categories = x.Categories,
				SystemPrompt = x.SystemPrompt,
				TelegramSession = x.TelegramSession == null
					? null
					: new ClassifierSessionInfo
					{
						Id = x.TelegramSession.Id,
						Name = x.TelegramSession.Name,
						IsActive = x.TelegramSession.IsActive
					},
				UpdatedAt = x.Updated ?? x.Created
			})
			.FirstOrDefaultAsync(ct);

	public Task<Guid?> GetTelegramSessionIdAsync(CancellationToken ct) =>
		context.ClassifierSettings
			.Where(x => x.Id == ClassifierSettings.SingletonId)
			.Select(x => x.TelegramSessionId)
			.FirstOrDefaultAsync(ct);

	public Task<bool> TelegramSessionBelongsToUserAsync(Guid userId, Guid sessionId, CancellationToken ct) =>
		context.TelegramSessions.AnyAsync(x => x.Id == sessionId && x.UserId == userId, ct);

	public async Task SaveClassifierSettingsAsync(UpdateClassifierSettingsCommand settings, CancellationToken ct)
	{
		var entity = await context.ClassifierSettings
			.FirstOrDefaultAsync(x => x.Id == ClassifierSettings.SingletonId, ct);
		if (entity is null)
		{
			entity = new ClassifierSettings
			{
				Id = ClassifierSettings.SingletonId,
				Model = settings.Model,
				SystemPrompt = settings.SystemPrompt
			};
			context.ClassifierSettings.Add(entity);
		}

		entity.IsEnabled = settings.IsEnabled;
		entity.Model = settings.Model;
		entity.BatchSize = settings.BatchSize;
		entity.IntervalMinutes = settings.IntervalMinutes;
		entity.MessageSampleCount = settings.MessageSampleCount;
		entity.PhotoCount = settings.PhotoCount;
		entity.ReclassifyAfterDays = settings.ReclassifyAfterDays;
		entity.Categories = [..settings.Categories];
		entity.SystemPrompt = settings.SystemPrompt;
		entity.TelegramSessionId = settings.TelegramSessionId;

		await context.SaveChangesAsync(ct);
	}
}
