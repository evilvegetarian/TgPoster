namespace TgPoster.API.Domain.UseCases.Repost.GetRepostSettings;

public interface IGetRepostSettingsStorage
{
	Task<GetRepostSettingsResponse?> GetRepostSettingsByScheduleIdAsync(Guid scheduleId, CancellationToken ct);
}
