using Shared.Enums;

namespace TgPoster.Worker.Domain.UseCases.CrossPosting.Publishing;

/// <summary>
///     Публикатор конкретной соцсети
/// </summary>
internal interface ISocialPublisher
{
	/// <summary>
	///     Поддерживаемая площадка
	/// </summary>
	SocialPlatform Platform { get; }

	/// <summary>
	///     Опубликовать кросс-пост
	/// </summary>
	/// <param name="request"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<SocialPublishResult> PublishAsync(SocialPublishRequest request, CancellationToken ct);
}
