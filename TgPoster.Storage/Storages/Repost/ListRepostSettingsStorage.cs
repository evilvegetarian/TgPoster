using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.Repost.ListRepostSettings;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Storages.Repost;

internal sealed class ListRepostSettingsStorage(PosterContext context) : IListRepostSettingsStorage
{
	public Task<List<RepostSettingsItemDto>> GetListAsync(Guid userId, CancellationToken ct)
	{
		return context.Set<RepostSettings>()
			.Where(x => x.Schedule.UserId == userId)
			.Select(x => new RepostSettingsItemDto
			{
				Id = x.Id,
				ScheduleId = x.ScheduleId,
				ScheduleName = x.Schedule.Name,
				TelegramSessionId = x.TelegramSessionId,
				IsActive = x.IsActive,
				DestinationsCount = x.Destinations.Count
			})
			.ToListAsync(ct);
	}
}
