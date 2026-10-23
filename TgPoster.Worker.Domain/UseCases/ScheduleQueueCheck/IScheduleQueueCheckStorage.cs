namespace TgPoster.Worker.Domain.UseCases.ScheduleQueueCheck;

public interface IScheduleQueueCheckStorage
{
	/// <summary>
	/// Возвращает очереди постов активных расписаний, у которых настроено хотя бы одно время публикации
	/// </summary>
	/// <param name="now"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<List<ScheduleQueueInfo>> GetActiveSchedulesQueueAsync(DateTimeOffset now, CancellationToken ct);
}
