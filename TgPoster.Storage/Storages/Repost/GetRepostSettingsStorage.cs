using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.Repost.CreateRepostSettings;
using TgPoster.API.Domain.UseCases.Repost.GetRepostSettings;
using TgPoster.Storage.Data;

namespace TgPoster.Storage.Storages.Repost;

internal sealed class GetRepostSettingsStorage(PosterContext context) : IGetRepostSettingsStorage
{
	public async Task<CreateRepostSettingsResponse?> GetRepostSettingsByScheduleIdAsync(Guid scheduleId,
		CancellationToken ct)
	{
		return await context.Set<Data.Entities.RepostSettings>()
			.Where(x => x.ScheduleId == scheduleId)
			.Select(x => new CreateRepostSettingsResponse
			{
				Id = x.Id,
				ScheduleId = x.ScheduleId,
				TelegramSessionId = x.TelegramSessionId,
				Destinations = x.Destinations.Select(d => new RepostDestinationDto
				{
					Id = d.Id,
					ChatIdentifier = d.ChatIdentifier,
					IsActive = d.IsActive
				}).ToList()
			})
			.FirstOrDefaultAsync(ct);
	}
}
