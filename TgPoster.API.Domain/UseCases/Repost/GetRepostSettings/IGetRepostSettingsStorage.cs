using TgPoster.API.Domain.UseCases.Repost.CreateRepostSettings;

namespace TgPoster.API.Domain.UseCases.Repost.GetRepostSettings;

public interface IGetRepostSettingsStorage
{
	Task<CreateRepostSettingsResponse?> GetRepostSettingsByScheduleIdAsync(Guid scheduleId, CancellationToken ct);
}
