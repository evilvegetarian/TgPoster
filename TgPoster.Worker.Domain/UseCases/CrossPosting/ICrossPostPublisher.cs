namespace TgPoster.Worker.Domain.UseCases.CrossPosting;

/// <summary>
///     Публикатор кросс-поста
/// </summary>
internal interface ICrossPostPublisher
{
	/// <summary>
	///     Опубликовать кросс-пост по id
	/// </summary>
	/// <param name="crossPostId"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task PublishAsync(Guid crossPostId, CancellationToken ct);
}
