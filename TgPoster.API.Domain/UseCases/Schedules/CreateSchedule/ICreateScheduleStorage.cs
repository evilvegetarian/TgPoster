namespace TgPoster.API.Domain.UseCases.Schedules.CreateSchedule;

public interface ICreateScheduleStorage
{
	Task<Guid> CreateScheduleAsync(
		string name,
		Guid userId,
		Guid telegramBot,
		long channelId,
		string userNameChat,
		Guid? youTubeAccountId,
		CancellationToken ct
	);

	Task<string?> GetApiTokenAsync(Guid telegramBotId, Guid userId, CancellationToken ct);
}