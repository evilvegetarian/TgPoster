namespace TgPoster.Worker.Domain.UseCases.CleanupChannels;

public interface IResumeChannelCleanupJobsStorage
{
	/// <summary>
	/// Найти незавершённые задания на очистку, чья сессия больше не ограничена Telegram
	/// </summary>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<List<Guid>> GetResumableJobIdsAsync(CancellationToken ct);
}
