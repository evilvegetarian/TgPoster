using TgPoster.API.Domain.Exceptions;

namespace TgPoster.API.Domain.Extensions;

public static class TelegramChannelExtension
{
	public static string ConvertToTelegramHandle(this string name)
	{
		name = name.Trim();

		if (string.IsNullOrWhiteSpace(name))
		{
			throw new InvalidTelegramChannelException();
		}

		var prefixes = new List<string>
		{
			"https://t.me/",
			"https://t.me"
		};

		if (name.StartsWith('@'))
		{
			return name;
		}

		foreach (var prefix in prefixes)
		{
			if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
			{
				var channel = name.Substring(prefix.Length).TrimStart('/').Trim();

				if (string.IsNullOrWhiteSpace(channel))
				{
					throw new InvalidTelegramChannelException();
				}

				return channel.StartsWith('@') ? channel : '@' + channel;
			}
		}

		return '@' + name;
	}
}