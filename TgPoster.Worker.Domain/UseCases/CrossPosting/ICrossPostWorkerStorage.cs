namespace TgPoster.Worker.Domain.UseCases.CrossPosting;

/// <summary>
///     Хранилище очереди кросс-постинга
/// </summary>
public interface ICrossPostWorkerStorage
{
	/// <summary>
	///     Поставить в очередь отправленные посты
	/// </summary>
	/// <param name="now"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<int> EnqueueDueAsync(DateTimeOffset now, CancellationToken ct);

	/// <summary>
	///     Пометить зависшие публикации как failed
	/// </summary>
	/// <param name="startedBefore"></param>
	/// <param name="error"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<int> FailStuckAsync(DateTimeOffset startedBefore, string error, CancellationToken ct);

	/// <summary>
	///     Пропустить осиротевшие кросс-посты
	/// </summary>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<int> SkipOrphanedAsync(CancellationToken ct);

	/// <summary>
	///     Взять следующий созревший кросс-пост
	/// </summary>
	/// <param name="now"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<Guid?> TakeNextDueAsync(DateTimeOffset now, CancellationToken ct);

	/// <summary>
	///     Получить данные для публикации
	/// </summary>
	/// <param name="crossPostId"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<CrossPostJobDto?> GetJobAsync(Guid crossPostId, CancellationToken ct);

	/// <summary>
	///     Пометить как опубликованный
	/// </summary>
	/// <param name="id"></param>
	/// <param name="externalPostId"></param>
	/// <param name="externalUrl"></param>
	/// <param name="publishedAt"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task MarkPublishedAsync(Guid id, string externalPostId, string? externalUrl, DateTimeOffset publishedAt, CancellationToken ct);

	/// <summary>
	///     Пометить как неудавшийся
	/// </summary>
	/// <param name="id"></param>
	/// <param name="error"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task MarkFailedAsync(Guid id, string error, CancellationToken ct);

	/// <summary>
	///     Пропустить кросс-пост
	/// </summary>
	/// <param name="id"></param>
	/// <param name="reason"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task MarkSkippedAsync(Guid id, string reason, CancellationToken ct);

	/// <summary>
	///     Перенести кросс-пост на новое время
	/// </summary>
	/// <param name="id"></param>
	/// <param name="scheduledAt"></param>
	/// <param name="error"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task RescheduleAsync(Guid id, DateTimeOffset scheduledAt, string error, CancellationToken ct);

	/// <summary>
	///     Пометить аккаунт как требующий переподключения
	/// </summary>
	/// <param name="accountId"></param>
	/// <param name="error"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task MarkAccountNeedsReauthAsync(Guid accountId, string error, CancellationToken ct);
}
