namespace TgPoster.Telegram.Models;

/// <summary>
/// Чаты, которые приложение использует через сессию: из них аккаунт выходить не должен.
/// Id сравниваются без префикса -100, username — без @, ссылки и регистра
/// </summary>
/// <param name="chatIds">Id чатов в любом формате (сырой, с -100 или с -)</param>
/// <param name="usernames">Username, @username или ссылка t.me</param>
public sealed class UsedTelegramChats(IEnumerable<long> chatIds, IEnumerable<string> usernames)
{
	private readonly HashSet<long> chatIds = chatIds.Select(TelegramChatId.ResolveRaw).ToHashSet();

	private readonly HashSet<string> usernames = usernames
		.Select(NormalizeUsername)
		.OfType<string>()
		.ToHashSet(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// Используется ли чат приложением
	/// </summary>
	/// <param name="chatId"></param>
	/// <param name="username"></param>
	/// <returns></returns>
	public bool Contains(long chatId, string? username)
	{
		if (chatIds.Contains(TelegramChatId.ResolveRaw(chatId)))
		{
			return true;
		}

		var normalized = NormalizeUsername(username);

		return normalized != null && usernames.Contains(normalized);
	}

	/// <summary>
	/// Приводит username, @username и ссылку вида https://t.me/username/123 к голому username
	/// </summary>
	/// <param name="value"></param>
	/// <returns></returns>
	private static string? NormalizeUsername(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return null;
		}

		var result = value.Trim();

		foreach (var prefix in (string[])["https://", "http://", "www."])
		{
			if (result.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
			{
				result = result[prefix.Length..];
			}
		}

		foreach (var host in (string[])["t.me/", "telegram.me/"])
		{
			if (result.StartsWith(host, StringComparison.OrdinalIgnoreCase))
			{
				result = result[host.Length..];
			}
		}

		result = result.TrimStart('@');

		var end = result.IndexOfAny(['/', '?']);
		if (end >= 0)
		{
			result = result[..end];
		}

		return result.Length == 0 ? null : result;
	}
}
