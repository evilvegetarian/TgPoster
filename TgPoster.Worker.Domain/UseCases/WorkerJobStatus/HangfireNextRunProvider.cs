using Hangfire;
using Hangfire.Storage;

namespace TgPoster.Worker.Domain.UseCases.WorkerJobStatus;

/// <summary>
///     Отдаёт время следующего запуска recurring job'а из Hangfire
/// </summary>
internal sealed class HangfireNextRunProvider(JobStorage jobStorage)
{
	/// <summary>
	///     Получить время следующего запуска job'а
	/// </summary>
	/// <param name="jobName">Имя job'а в Hangfire</param>
	/// <returns>Время следующего запуска в UTC или null, если job не зарегистрирован</returns>
	public DateTimeOffset? GetNextRunAt(string jobName)
	{
		using var connection = jobStorage.GetConnection();
		var job = connection.GetRecurringJobs().FirstOrDefault(x => x.Id == jobName);
		return job?.NextExecution is { } next
			? new DateTimeOffset(next, TimeSpan.Zero)
			: null;
	}
}