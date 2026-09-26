using Shared.Enums;

namespace Shared.CrossPosting;

/// <summary>
///     Правила площадки по длине и особенностям текста
/// </summary>
/// <param name="MaxLength"></param>
/// <param name="Mode"></param>
/// <param name="SupportsChain"></param>
/// <param name="ShortLinkText"></param>
/// <param name="RequiresMedia"></param>
public sealed record PlatformTextProfile(
	int MaxLength,
	TextLengthMode Mode,
	bool SupportsChain,
	bool ShortLinkText,
	bool RequiresMedia)
{
	/// <summary>
	///     Получить правила площадки
	/// </summary>
	/// <param name="platform"></param>
	/// <returns></returns>
	public static PlatformTextProfile For(SocialPlatform platform)
	{
		return platform switch
		{
			SocialPlatform.Bluesky => new PlatformTextProfile(300, TextLengthMode.Graphemes, true, true, false),
			_ => throw new ArgumentOutOfRangeException(nameof(platform), platform, null)
		};
	}
}
