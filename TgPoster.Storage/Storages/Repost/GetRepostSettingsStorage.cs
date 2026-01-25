using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.Repost.GetRepostSettings;
using TgPoster.Storage.Data;

namespace TgPoster.Storage.Storages.Repost;

internal sealed class GetRepostSettingsStorage(PosterContext context) : IGetRepostSettingsStorage
{
	public async Task<GetRepostSettingsResponse?> GetRepostSettingsByScheduleIdAsync(Guid scheduleId,
		CancellationToken ct)
	{
		return await context.Set<Data.Entities.RepostSettings>()
			.Where(x => x.ScheduleId == scheduleId)
			.Select(x => new GetRepostSettingsResponse
			{
				Id = x.Id,
				ScheduleId = x.ScheduleId,
				TelegramSessionId = x.TelegramSessionId,
				Destinations = x.Destinations.Select(d => new GetRepostDestinationDto
				{
					Id = d.Id,
					ChatIdentifier = d.ChatId,
					IsActive = d.IsActive
				}).ToList()
			})
			.FirstOrDefaultAsync(ct);
	}
}
