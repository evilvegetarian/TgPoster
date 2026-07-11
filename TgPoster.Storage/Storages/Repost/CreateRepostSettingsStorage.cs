using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.Repost.CreateRepostSettings;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Storages.Repost;

internal sealed class CreateRepostSettingsStorage(PosterContext context, GuidFactory guidFactory)
	: ICreateRepostSettingsStorage
{
	public Task<bool> ScheduleExistsAsync(Guid scheduleId, CancellationToken ct)
	{
		return context.Schedules.AnyAsync(x => x.Id == scheduleId, ct);
	}

	public Task<bool> TelegramSessionExistsAndActiveAsync(Guid telegramSessionId, CancellationToken ct)
	{
		return context.TelegramSessions.AnyAsync(x => x.Id == telegramSessionId && x.IsActive, ct);
	}

	public async Task<Guid> CreateRepostSettingsAsync(
		Guid scheduleId,
		Guid telegramSessionId,
		List<long> destinations,
		CancellationToken ct
	)
	{
		var settingsId = guidFactory.New();
		var settings = new RepostSettings
		{
			Id = settingsId,
			ScheduleId = scheduleId,
			TelegramSessionId = telegramSessionId,
			IsActive = true
		};

		await context.AddAsync(settings, ct);

		// Каналы наследуют общие настройки создаваемых RepostSettings — тем же правилом,
		// что и при добавлении канала в существующие настройки
		var destinationEntities = destinations.Select(d => new RepostDestination
		{
			Id = guidFactory.New(),
			RepostSettingsId = settingsId,
			ChatId = d,
			IsActive = true,
			DelayMinSeconds = settings.DefaultDelayMinSeconds,
			DelayMaxSeconds = settings.DefaultDelayMaxSeconds,
			RepostEveryNth = settings.DefaultRepostEveryNth,
			SkipProbability = settings.DefaultSkipProbability,
			MaxRepostsPerDay = settings.DefaultMaxRepostsPerDay
		}).ToList();

		await context.AddRangeAsync(destinationEntities, ct);
		await context.SaveChangesAsync(ct);

		return settingsId;
	}
}