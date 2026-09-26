using System.Net;
using System.Text.RegularExpressions;

namespace Shared.CrossPosting;

/// <summary>
///     Перевод Telegram-HTML в обычный текст для соцсетей
/// </summary>
public static partial class TelegramHtmlToText
{
	private const string SpoilerPlaceholder = "[спойлер]";

	/// <summary>
	///     Преобразовать Telegram-HTML в обычный текст
	/// </summary>
	/// <param name="html"></param>
	/// <returns></returns>
	public static string Convert(string? html)
	{
		if (string.IsNullOrWhiteSpace(html))
		{
			return string.Empty;
		}

		var text = NewLineRegex().Replace(html, "\n");
		text = BreakRegex().Replace(text, "\n");
		text = SpoilerTagRegex().Replace(text, SpoilerPlaceholder);
		text = SpoilerSpanRegex().Replace(text, SpoilerPlaceholder);
		text = TagRegex().Replace(text, string.Empty);
		text = WebUtility.HtmlDecode(text);
		text = TrailingWhitespaceRegex().Replace(text, string.Empty);
		text = ExcessiveNewLinesRegex().Replace(text, "\n\n");

		return text.Trim();
	}

	[GeneratedRegex(@"\r\n|\r")]
	private static partial Regex NewLineRegex();

	[GeneratedRegex("<br\\s*/?>", RegexOptions.IgnoreCase)]
	private static partial Regex BreakRegex();

	[GeneratedRegex("<tg-spoiler>.*?</tg-spoiler>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
	private static partial Regex SpoilerTagRegex();

	[GeneratedRegex("<span\\s+class=\"tg-spoiler\">.*?</span>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
	private static partial Regex SpoilerSpanRegex();

	[GeneratedRegex("<[^<>]*>")]
	private static partial Regex TagRegex();

	[GeneratedRegex("[ \t]+(?=\n)")]
	private static partial Regex TrailingWhitespaceRegex();

	[GeneratedRegex("\n{3,}")]
	private static partial Regex ExcessiveNewLinesRegex();
}
