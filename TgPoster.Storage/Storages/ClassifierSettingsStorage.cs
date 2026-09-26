using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using TgPoster.API.Domain.UseCases.Discover.GetClassifierSettings;
using TgPoster.API.Domain.UseCases.Discover.UpdateClassifierSettings;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.Enum;

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
				UpdatedAt = x.Updated ?? x.Created
			})
			.FirstOrDefaultAsync(ct);

	public Task<List<ClassifierSessionOption>> GetClassifierSessionsAsync(Guid userId, CancellationToken ct) =>
		context.TelegramSessions
			.Where(x => x.UserId == userId || x.Purposes.Contains(TelegramSessionPurpose.Classification))
			.OrderBy(x => x.UserId != userId)
			.ThenBy(x => x.Created)
			.Select(x => new ClassifierSessionOption
			{
				Id = x.Id,
				Name = x.Name,
				PhoneNumber = x.UserId == userId ? x.PhoneNumber : null,
				IsActive = x.IsActive,
				IsAuthorized = x.Status == TelegramSessionStatus.Authorized,
				IsSelected = x.Purposes.Contains(TelegramSessionPurpose.Classification),
				IsOwn = x.UserId == userId
			})
			.ToListAsync(ct);

	public Task<List<Guid>> GetUserSessionIdsAsync(Guid userId, CancellationToken ct) =>
		context.TelegramSessions
			.Where(x => x.UserId == userId)
			.Select(x => x.Id)
			.ToListAsync(ct);

	public async Task SaveClassifierSettingsAsync(
		UpdateClassifierSettingsCommand settings,
		Guid userId,
		CancellationToken ct
	)
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

		var selected = settings.TelegramSessionIds.ToHashSet();
		var userSessions = await context.TelegramSessions
			.Where(x => x.UserId == userId)
			.ToListAsync(ct);
		foreach (var session in userSessions)
		{
			var hasPurpose = session.Purposes.Contains(TelegramSessionPurpose.Classification);
			if (selected.Contains(session.Id) && !hasPurpose)
			{
				session.Purposes = [..session.Purposes, TelegramSessionPurpose.Classification];
			}
			else if (!selected.Contains(session.Id) && hasPurpose)
			{
				session.Purposes = session.Purposes
					.Where(x => x != TelegramSessionPurpose.Classification)
					.ToArray();
			}
		}

		await context.SaveChangesAsync(ct);
	}
}
