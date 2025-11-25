namespace TgPoster.API.Domain.UseCases.OpenRouterSetting.AddScheduleOpenRouterSetting;

public interface IAddScheduleOpenRouterSettingStorage
{
	Task<bool> ExistScheduleAsync(Guid scheduleId, Guid userId, CancellationToken ctx);
	Task<bool> ExistOpenRouterAsync(Guid openRouterId, Guid userId, CancellationToken ctx);
	Task UpdateOpenRouterAsync(Guid openRouterId, Guid scheduleId, CancellationToken ctx);
}