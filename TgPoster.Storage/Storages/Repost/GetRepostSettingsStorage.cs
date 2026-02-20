using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.Repost.GetRepostSettings;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Storages.Repost;

internal sealed class GetRepostSettingsStorage(PosterContext context) : IGetRepostSettingsStorage
{
	public Task<RepostSettingsResponse?> GetAsync(Guid id, Guid userId, CancellationToken ct)
	{
		return context.Set<RepostSettings>()
			.Where(x => x.Id == id && x.Schedule.UserId == userId)
			.Select(x => new RepostSettingsResponse
			{
				Id = x.Id,
				ScheduleId = x.ScheduleId,
				ScheduleName = x.Schedule.Name,
				TelegramSessionId = x.TelegramSessionId,
				TelegramSessionName = x.TelegramSession.Name,
				IsActive = x.IsActive,
				Created = x.Created!.Value,
				Destinations = x.Destinations.Select(d => new RepostDestinationDto
				{
					Id = d.Id,
					ChatId = d.ChatId,
					IsActive = d.IsActive,
					Title = d.Title,
					Username = d.Username,
					MemberCount = d.MemberCount,
					ChatType = d.ChatType,
					ChatStatus = d.ChatStatus,
					AvatarBase64 = d.AvatarBase64,
					InfoUpdatedAt = d.InfoUpdatedAt
				}).ToList()
			})
			.FirstOrDefaultAsync(ct);
	}
}
