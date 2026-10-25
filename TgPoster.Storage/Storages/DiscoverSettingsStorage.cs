using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using TgPoster.API.Domain.UseCases.Discover.GetDiscoverSettings;
using TgPoster.API.Domain.UseCases.Discover.UpdateDiscoverSettings;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.Enum;

namespace TgPoster.Storage.Storages;

internal sealed class DiscoverSettingsStorage(PosterContext context)
	: IGetDiscoverSettingsStorage, IUpdateDiscoverSettingsStorage
{
	public Task<SavedDiscoverSettingsDto?> GetDiscoverSettingsAsync(CancellationToken ct) =>
		context.DiscoverSettings
			.Where(x => x.Id == DiscoverSettings.SingletonId)
			.Select(x => new SavedDiscoverSettingsDto
			{
				IsEnabled = x.IsEnabled,
				IntervalMinutes = x.IntervalMinutes,
				ChannelsPerSession = x.ChannelsPerSession,
				UpdatedAt = x.Updated ?? x.Created
			})
			.FirstOrDefaultAsync(ct);

	public Task<List<DiscoverSessionOption>> GetDiscoverSessionsAsync(Guid userId, CancellationToken ct) =>
		context.TelegramSessions
			.Where(x => x.UserId == userId || x.Purposes.Contains(TelegramSessionPurpose.Discover))
			.OrderBy(x => x.UserId != userId)
			.ThenBy(x => x.Created)
			.Select(x => new DiscoverSessionOption
			{
				Id = x.Id,
				Name = x.Name,
				PhoneNumber = x.UserId == userId ? x.PhoneNumber : null,
				IsActive = x.IsActive,
				IsAuthorized = x.Status == TelegramSessionStatus.Authorized,
				IsSelected = x.Purposes.Contains(TelegramSessionPurpose.Discover),
				IsOwn = x.UserId == userId
			})
			.ToListAsync(ct);

	public Task<List<Guid>> GetUserSessionIdsAsync(Guid userId, CancellationToken ct) =>
		context.TelegramSessions
			.Where(x => x.UserId == userId)
			.Select(x => x.Id)
			.ToListAsync(ct);

	public async Task SaveDiscoverSettingsAsync(
		UpdateDiscoverSettingsCommand settings,
		Guid userId,
		CancellationToken ct
	)
	{
		var entity = await context.DiscoverSettings
			.FirstOrDefaultAsync(x => x.Id == DiscoverSettings.SingletonId, ct);
		if (entity is null)
		{
			entity = new DiscoverSettings { Id = DiscoverSettings.SingletonId };
			context.DiscoverSettings.Add(entity);
		}

		entity.IsEnabled = settings.IsEnabled;
		entity.IntervalMinutes = settings.IntervalMinutes;
		entity.ChannelsPerSession = settings.ChannelsPerSession;

		var selected = settings.TelegramSessionIds.ToHashSet();
		var userSessions = await context.TelegramSessions
			.Where(x => x.UserId == userId)
			.ToListAsync(ct);
		foreach (var session in userSessions)
		{
			var hasPurpose = session.Purposes.Contains(TelegramSessionPurpose.Discover);
			if (selected.Contains(session.Id) && !hasPurpose)
			{
				session.Purposes = [..session.Purposes, TelegramSessionPurpose.Discover];
			}
			else if (!selected.Contains(session.Id) && hasPurpose)
			{
				session.Purposes = session.Purposes
					.Where(x => x != TelegramSessionPurpose.Discover)
					.ToArray();
			}
		}

		await context.SaveChangesAsync(ct);
	}
}
