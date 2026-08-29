using System.Text.RegularExpressions;

namespace Shared.Utilities;

/// <summary>
///     Лимиты Telegram на длину текста публикации
/// </summary>
public static class TelegramLimits
{
	/// <summary>
	///     Максимальная длина подписи к медиа
	/// </summary>
	public const int CaptionLength = 1024;

	/// <summary>
	///     Максимальная длина текстового сообщения
	/// </summary>
	public const int MessageLength = 4096;
}

/// <summary>
///     Приклеивает общую подпись расписания к тексту поста и проверяет её разметку
/// </summary>
public static partial class PostSignatureComposer
{
	private const string Separator = "\n\n";
	private const string Ellipsis = "…";

	/// <summary>
	///     Теги, которые понимает Telegram в режиме HTML
	/// </summary>
	private static readonly HashSet<string> allowedTags = new(StringComparer.OrdinalIgnoreCase)
	{
		"b", "strong", "i", "em", "u", "ins", "s", "strike", "del",
		"a", "code", "pre", "span", "tg-spoiler", "blockquote"
	};

	/// <summary>
	///     Склеивает текст поста с подписью снизу, укладываясь в лимит
	/// </summary>
	/// <param name="text">Текст поста</param>
	/// <param name="signature">Подпись расписания</param>
	/// <param name="limit">Максимально допустимая длина результата</param>
	/// <returns>
	///     Текст с подписью снизу. Если склейка не влезает в лимит, обрезается текст поста, а подпись сохраняется
	/// </returns>
	public static string Compose(string? text, string? signature, int limit)
	{
		var body = text ?? string.Empty;
		if (string.IsNullOrWhiteSpace(signature))
		{
			return body;
		}

		var footer = signature.Trim();
		if (string.IsNullOrWhiteSpace(body))
		{
			return footer;
		}

		body = body.TrimEnd();
		var combined = body + Separator + footer;
		if (combined.Length <= limit)
		{
			return combined;
		}

		var available = limit - Separator.Length - footer.Length - Ellipsis.Length;
		if (available <= 0)
		{
			return footer;
		}

		var truncated = TrimDanglingTag(body[..available]).TrimEnd();
		return truncated.Length == 0
			? footer
			: truncated + Ellipsis + Separator + footer;
	}

	/// <summary>
	///     Проверяет, что подпись является валидным для Telegram HTML
	/// </summary>
	/// <param name="html">Проверяемая разметка</param>
	/// <returns>
	///     <c>true</c>, если разметка пуста либо состоит только из поддерживаемых и корректно закрытых тегов
	/// </returns>
	public static bool IsValidTelegramHtml(string? html)
	{
		if (string.IsNullOrWhiteSpace(html))
		{
			return true;
		}

		var matches = TagRegex().Matches(html);

		// Каждый символ "<" обязан быть началом тега: одиночный "<" ломает разбор на стороне Telegram
		if (matches.Count != html.Count(symbol => symbol == '<'))
		{
			return false;
		}

		var opened = new Stack<string>();
		foreach (Match match in matches)
		{
			var name = match.Groups["name"].Value;
			if (!allowedTags.Contains(name))
			{
				return false;
			}

			if (match.Groups["close"].Value.Length == 0)
			{
				opened.Push(name);
				continue;
			}

			if (opened.Count == 0 || !opened.Pop().Equals(name, StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
		}

		return opened.Count == 0;
	}

	/// <summary>
	///     Отбрасывает незавершённый HTML-тег в хвотсе обрезанного текста
	/// </summary>
	/// <param name="value">Обрезанный текст поста</param>
	/// <returns>Текст без повисшего открывающего тега</returns>
	private static string TrimDanglingTag(string value)
	{
		var lastOpen = value.LastIndexOf('<');
		if (lastOpen < 0)
		{
			return value;
		}

		var lastClose = value.LastIndexOf('>');
		return lastOpen > lastClose ? value[..lastOpen] : value;
	}

	[GeneratedRegex("<(?<close>/?)(?<name>[a-zA-Z][a-zA-Z0-9-]*)(?<attrs>[^<>]*)>")]
	private static partial Regex TagRegex();
}
