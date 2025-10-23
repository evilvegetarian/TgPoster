namespace TgPoster.API.Domain.Extensions;

public static class TelegramChannelExtension
{
	public static string ConvertToTelegramHandle(this string name)
	{
		name = name.Trim();

		if (string.IsNullOrWhiteSpace(name))
		{
			throw new ArgumentException("Channel cannot be empty");
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
					throw new ArgumentException("Channel cannot be empty");
				}

				return channel.StartsWith('@') ? channel : '@' + channel;
			}
		}

		return '@' + name;
	}
}