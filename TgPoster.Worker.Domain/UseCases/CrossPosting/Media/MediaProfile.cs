using Shared.Enums;

namespace TgPoster.Worker.Domain.UseCases.CrossPosting.Media;

/// <summary>
///     Профиль требований площадки к медиа
/// </summary>
internal sealed record MediaProfile(int MaxImages, long MaxImageBytes, double? MinAspect, double? MaxAspect)
{
	/// <summary>
	///     Получить профиль для указанной площадки
	/// </summary>
	/// <param name="platform"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentOutOfRangeException"></exception>
	public static MediaProfile For(SocialPlatform platform)
	{
		return platform switch
		{
			SocialPlatform.Bluesky => new MediaProfile(4, 1_000_000, null, null),
			_ => throw new ArgumentOutOfRangeException(nameof(platform), platform, null)
		};
	}
}
