using Shared.Enums;

namespace Shared.CrossPosting;

/// <summary>
///     Сборка ссылки на Telegram для кросс-поста
/// </summary>
public static class CrossPostLinkBuilder
{
	/// <summary>
	///     Построить ссылку по цели и данным канала
	/// </summary>
	/// <param name="target"></param>
	/// <param name="channelName"></param>
	/// <param name="telegramMessageId"></param>
	/// <param name="customLink"></param>
	/// <returns></returns>
	public static string? Build(
		CrossPostLinkTarget target,
		string channelName,
		int? telegramMessageId,
		string? customLink)
	{
		var channel = channelName.Trim().TrimStart('@');

		return target switch
		{
			CrossPostLinkTarget.Post => telegramMessageId is null
				? $"https://t.me/{channel}"
				: $"https://t.me/{channel}/{telegramMessageId}",
			CrossPostLinkTarget.Channel => $"https://t.me/{channel}",
			CrossPostLinkTarget.Custom => string.IsNullOrWhiteSpace(customLink) ? null : customLink,
			CrossPostLinkTarget.None => null,
			_ => throw new ArgumentOutOfRangeException(nameof(target), target, null)
		};
	}

	/// <summary>
	///     Подготовить текст ссылки для площадки
	/// </summary>
	/// <param name="link"></param>
	/// <param name="profile"></param>
	/// <returns></returns>
	public static string ToLinkText(string link, PlatformTextProfile profile)
	{
		if (!profile.ShortLinkText)
		{
			return link;
		}

		const string https = "https://";
		const string http = "http://";

		if (link.StartsWith(https, StringComparison.OrdinalIgnoreCase))
		{
			return link[https.Length..];
		}

		if (link.StartsWith(http, StringComparison.OrdinalIgnoreCase))
		{
			return link[http.Length..];
		}

		return link;
	}
}
