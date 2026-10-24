using System.Text.RegularExpressions;
using TgPoster.Telegram.Models;

namespace TgPoster.Worker.Domain.Services.AdDetection;

/// <summary>
/// Эвристики для распознавания рекламных постов по тексту и ссылкам сообщения
/// </summary>
internal static class AdDetector
{
	public const string EridRule = "Erid";
	public const string HashtagRule = "Hashtag";
	public const string MarkingRule = "Marking";
	public const string PhraseRule = "Phrase";
	public const string BotReferralRule = "BotReferral";

	private const RegexOptions Options =
		RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;

	private static readonly AdDetectionResult NotAd = new(false, null);

	private static readonly Regex EridInTextRegex = new(@"\berid\s*[:=]\s*[\w-]{5,}", Options);

	private static readonly Regex EridInUrlRegex = new(@"[?&]erid=[^&#\s]", Options);

	private static readonly Regex BareUrlRegex = new(@"https?://\S+", Options);

	private static readonly Regex HashtagRegex =
		new(@"(?<!\w)#(?:реклама|промо|спонсор|партн[её]р|ads?|sponsored)(?![\p{L}\p{N}_])", Options);

	// Строка маркировки может начинаться с эмодзи или кавычки, поэтому до слова допускаются не буквы и не цифры
	private static readonly Regex MarkingLineRegex = new(
		@"^[^\p{L}\p{N}\r\n]*реклама[^\S\r\n]*(?:[.:]|[,—–-]?[^\S\r\n]*(?:ООО|ИП|АО|ПАО|ЗАО)(?![\p{L}\p{N}_]))",
		Options | RegexOptions.Multiline);

	private static readonly Regex AdWordRegex = new(@"(?<![\p{L}\p{N}_])реклама(?![\p{L}\p{N}_])", Options);

	private static readonly Regex InnRegex =
		new(@"(?<![\p{L}\p{N}_])ИНН[^\S\r\n]*:?[^\S\r\n]*\d{10}(?:\d{2})?(?!\d)", Options);

	// «Это не рекламный пост» — оговорка автора, а не маркировка
	private static readonly Regex PhraseRegex = new(
		@"(?<![\p{L}\p{N}_])(?<!(?<![\p{L}\p{N}_])не\s+)(?:на\s+правах\s+рекламы|партн[её]рский\s+(?:пост|материал)|рекламный\s+пост|спонсор\s+поста)(?![\p{L}\p{N}_])",
		Options);

	private static readonly Regex BotReferralRegex = new(
		@"^(?:https?://)?(?:www\.)?(?:t|telegram)\.me/[a-z0-9_]*bot(?:/[^?#\s]*)?\?(?:[^#\s]*&)?start=",
		Options);

	/// <summary>
	/// Проверяет текст сообщения и его ссылки на признаки рекламы. Правила: маркировка erid в тексте или
	/// в ссылке, рекламные хэштеги, строка маркировки «Реклама. ООО …» или слово «реклама» вместе с ИНН,
	/// фразы вроде «на правах рекламы», реферальная ссылка на бота со start-параметром
	/// </summary>
	/// <param name="text"></param>
	/// <param name="entities"></param>
	/// <returns></returns>
	public static AdDetectionResult Detect(string? text, IReadOnlyList<TelegramMessageEntity>? entities)
	{
		text ??= string.Empty;
		var urls = CollectUrls(text, entities);

		if (EridInTextRegex.IsMatch(text) || urls.Any(url => EridInUrlRegex.IsMatch(url)))
		{
			return new AdDetectionResult(true, EridRule);
		}

		if (HashtagRegex.IsMatch(text))
		{
			return new AdDetectionResult(true, HashtagRule);
		}

		if (MarkingLineRegex.IsMatch(text) || (AdWordRegex.IsMatch(text) && InnRegex.IsMatch(text)))
		{
			return new AdDetectionResult(true, MarkingRule);
		}

		if (PhraseRegex.IsMatch(text))
		{
			return new AdDetectionResult(true, PhraseRule);
		}

		if (urls.Any(url => BotReferralRegex.IsMatch(url)))
		{
			return new AdDetectionResult(true, BotReferralRule);
		}

		return NotAd;
	}

	/// <summary>
	/// Собирает ссылки сообщения: видимые ссылки по сущнстям (смещения в UTF-16, как и у string),
	/// скрытые ссылки TextUrl и голые http-ссылки в тексте
	/// </summary>
	/// <param name="text"></param>
	/// <param name="entities"></param>
	/// <returns></returns>
	private static List<string> CollectUrls(string text, IReadOnlyList<TelegramMessageEntity>? entities)
	{
		List<string> urls = [];
		if (entities is not null)
		{
			foreach (var entity in entities)
			{
				switch (entity.Type)
				{
					case TelegramMessageEntityType.TextUrl when !string.IsNullOrEmpty(entity.Url):
						urls.Add(entity.Url);
						break;
					case TelegramMessageEntityType.Url
						when entity.Offset >= 0 && entity.Length > 0 && entity.Offset <= text.Length - entity.Length:
						urls.Add(text.Substring(entity.Offset, entity.Length));
						break;
				}
			}
		}

		urls.AddRange(BareUrlRegex.Matches(text).Select(match => match.Value));
		return urls;
	}
}
