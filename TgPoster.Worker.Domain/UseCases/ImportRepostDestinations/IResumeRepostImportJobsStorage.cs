namespace TgPoster.Worker.Domain.UseCases.ImportRepostDestinations;

public interface IResumeRepostImportJobsStorage
{
	/// <summary>
	///     Найти незавершённые задания, чья сессия больше не ограничена Telegram
	/// </summary>
	/// <param name="ct">Токен отмены</param>
	/// <returns>Id заданий, готовых к продолжению</returns>
	Task<List<Guid>> GetResumableJobIdsAsync(CancellationToken ct);
}
