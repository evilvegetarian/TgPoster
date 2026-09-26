using System.Text.RegularExpressions;
using Shared.Enums;

namespace Shared.CrossPosting;

/// <summary>
///     Сборка текста кросс-поста с учётом лимитов площадки
/// </summary>
public static partial class CrossPostComposer
{
	/// <summary>
	///     Максимум частей в цепочке, больше — публикуется тизер
	/// </summary>
	public const int MaxChainParts = 8;

	private const string Separator = "\n\n";
	private const string Ellipsis = "…";
	private const string Space = " ";

	/// <summary>
	///     Собрать текст кросс-поста в одну или несколько частей
	/// </summary>
	/// <param name="text"></param>
	/// <param name="format"></param>
	/// <param name="linkText"></param>
	/// <param name="callToAction"></param>
	/// <param name="profile"></param>
	/// <returns></returns>
	public static CrossPostComposition Compose(
		string text,
		CrossPostFormat format,
		string? linkText,
		string callToAction,
		PlatformTextProfile profile)
	{
		var footer = BuildFooter(callToAction, linkText);
		var max = profile.MaxLength;
		var mode = profile.Mode;

		if (TextLength.Measure(footer, mode) > max)
		{
			var truncated = HardTruncate(footer, max, mode);
			return new CrossPostComposition([truncated], true, false);
		}

		return format switch
		{
			CrossPostFormat.Teaser => ComposeTeaser(text, footer, max, mode),
			CrossPostFormat.Full => ComposeFull(text, footer, max, mode, profile.SupportsChain),
			CrossPostFormat.Announcement => new CrossPostComposition([footer], false, false),
			_ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
		};
	}

	private static string BuildFooter(string callToAction, string? linkText)
	{
		return linkText is null ? callToAction : callToAction + "\n" + linkText;
	}

	private static CrossPostComposition ComposeTeaser(string text, string footer, int max, TextLengthMode mode)
	{
		if (string.IsNullOrEmpty(text))
		{
			return new CrossPostComposition([footer], false, false);
		}

		var full = text + Separator + footer;
		if (TextLength.Measure(full, mode) <= max)
		{
			return new CrossPostComposition([full], false, false);
		}

		var suffix = Separator + footer;
		var suffixLength = TextLength.Measure(suffix, mode);
		var ellipsisLength = TextLength.Measure(Ellipsis, mode);
		var available = max - suffixLength - ellipsisLength;

		if (available < 1)
		{
			return new CrossPostComposition([footer], true, false);
		}

		var prefix = TruncateAtWord(text, available, mode);
		var result = prefix + Ellipsis + suffix;
		return new CrossPostComposition([result], true, false);
	}

	private static CrossPostComposition ComposeFull(
		string text,
		string footer,
		int max,
		TextLengthMode mode,
		bool supportsChain)
	{
		if (string.IsNullOrEmpty(text))
		{
			return new CrossPostComposition([footer], false, false);
		}

		if (TextLength.Measure(text + Separator + footer, mode) <= max)
		{
			return new CrossPostComposition([text + Separator + footer], false, false);
		}

		if (!supportsChain)
		{
			return ComposeTeaser(text, footer, max, mode);
		}

		var parts = SplitIntoChunks(text, max, mode);

		if (parts.Count == 0)
		{
			return new CrossPostComposition([footer], false, false);
		}

		var lastIndex = parts.Count - 1;
		var lastWithFooter = parts[lastIndex] + Separator + footer;
		if (TextLength.Measure(lastWithFooter, mode) <= max)
		{
			parts[lastIndex] = lastWithFooter;
		}
		else
		{
			parts.Add(footer);
		}

		if (parts.Count > MaxChainParts)
		{
			return ComposeTeaser(text, footer, max, mode) with { FellBackToTeaser = true };
		}

		return new CrossPostComposition(parts, false, false);
	}

	private static string TruncateAtWord(string text, int maxLength, TextLengthMode mode)
	{
		var elements = TextLength.SplitTextElements(text);
		var prefix = "";
		var prefixElementCount = 0;

		foreach (var element in elements)
		{
			var candidate = prefix + element;
			if (TextLength.Measure(candidate, mode) > maxLength)
			{
				break;
			}

			prefix = candidate;
			prefixElementCount++;
		}

		if (prefixElementCount >= elements.Count)
		{
			return prefix.TrimEnd();
		}

		var nextElement = elements[prefixElementCount];
		if (IsWhitespace(nextElement) || EndsWithWhitespace(prefix))
		{
			return prefix.TrimEnd().TrimEnd(',', ';', ':', '—', '–', '-').TrimEnd();
		}

		var lastSpaceIndex = LastIndexOfWhitespace(prefix);
		var halfLength = prefix.Length / 2;
		if (lastSpaceIndex >= halfLength)
		{
			prefix = prefix[..lastSpaceIndex];
		}

		return prefix.TrimEnd().TrimEnd(',', ';', ':', '—', '–', '-').TrimEnd();
	}

	private static List<string> SplitIntoChunks(string text, int maxLength, TextLengthMode mode)
	{
		var parts = new List<string>();
		var current = "";
		var paragraphs = text.Split(Separator, StringSplitOptions.None);

		foreach (var paragraph in paragraphs)
		{
			if (TryAppend(ref current, paragraph, Separator, maxLength, mode))
			{
				continue;
			}

			if (!string.IsNullOrEmpty(current))
			{
				parts.Add(current);
				current = "";
			}

			if (TextLength.Measure(paragraph, mode) <= maxLength)
			{
				current = paragraph;
			}
			else
			{
				var sentenceChunks = PackSentences(paragraph, maxLength, mode);
				for (var i = 0; i < sentenceChunks.Count - 1; i++)
				{
					parts.Add(sentenceChunks[i]);
				}

				current = sentenceChunks.Count > 0 ? sentenceChunks[^1] : "";
			}
		}

		if (!string.IsNullOrEmpty(current))
		{
			parts.Add(current);
		}

		return parts;
	}

	private static List<string> PackSentences(string paragraph, int maxLength, TextLengthMode mode)
	{
		var parts = new List<string>();
		var current = "";
		var sentences = SentenceSplitRegex().Split(paragraph);

		foreach (var sentence in sentences)
		{
			if (TryAppend(ref current, sentence, Space, maxLength, mode))
			{
				continue;
			}

			if (!string.IsNullOrEmpty(current))
			{
				parts.Add(current);
				current = "";
			}

			if (TextLength.Measure(sentence, mode) <= maxLength)
			{
				current = sentence;
			}
			else
			{
				var wordChunks = PackWords(sentence, maxLength, mode);
				for (var i = 0; i < wordChunks.Count - 1; i++)
				{
					parts.Add(wordChunks[i]);
				}

				current = wordChunks.Count > 0 ? wordChunks[^1] : "";
			}
		}

		if (!string.IsNullOrEmpty(current))
		{
			parts.Add(current);
		}

		return parts;
	}

	private static List<string> PackWords(string sentence, int maxLength, TextLengthMode mode)
	{
		var parts = new List<string>();
		var current = "";
		var words = sentence.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

		foreach (var word in words)
		{
			if (TryAppend(ref current, word, Space, maxLength, mode))
			{
				continue;
			}

			if (!string.IsNullOrEmpty(current))
			{
				parts.Add(current);
				current = "";
			}

			if (TextLength.Measure(word, mode) <= maxLength)
			{
				current = word;
			}
			else
			{
				var hardChunks = HardCut(word, maxLength, mode);
				for (var i = 0; i < hardChunks.Count - 1; i++)
				{
					parts.Add(hardChunks[i]);
				}

				current = hardChunks.Count > 0 ? hardChunks[^1] : "";
			}
		}

		if (!string.IsNullOrEmpty(current))
		{
			parts.Add(current);
		}

		return parts;
	}

	private static List<string> HardCut(string text, int maxLength, TextLengthMode mode)
	{
		var parts = new List<string>();
		var current = "";
		var elements = TextLength.SplitTextElements(text);

		foreach (var element in elements)
		{
			var candidate = current + element;
			if (TextLength.Measure(candidate, mode) > maxLength)
			{
				if (!string.IsNullOrEmpty(current))
				{
					parts.Add(current);
				}

				current = element;
			}
			else
			{
				current = candidate;
			}
		}

		if (!string.IsNullOrEmpty(current))
		{
			parts.Add(current);
		}

		return parts;
	}

	private static string HardTruncate(string text, int maxLength, TextLengthMode mode)
	{
		var current = "";
		var elements = TextLength.SplitTextElements(text);

		foreach (var element in elements)
		{
			var candidate = current + element;
			if (TextLength.Measure(candidate, mode) > maxLength)
			{
				break;
			}

			current = candidate;
		}

		return current;
	}

	private static bool TryAppend(ref string current, string item, string separator, int maxLength, TextLengthMode mode)
	{
		var candidate = string.IsNullOrEmpty(current) ? item : current + separator + item;
		if (TextLength.Measure(candidate, mode) <= maxLength)
		{
			current = candidate;
			return true;
		}

		return false;
	}

	private static bool IsWhitespace(string text)
	{
		return text.Length == 1 && char.IsWhiteSpace(text[0]);
	}

	private static bool EndsWithWhitespace(string text)
	{
		return text.Length > 0 && char.IsWhiteSpace(text[^1]);
	}

	private static int LastIndexOfWhitespace(string text)
	{
		for (var i = text.Length - 1; i >= 0; i--)
		{
			if (char.IsWhiteSpace(text[i]))
			{
				return i;
			}
		}

		return -1;
	}

	[GeneratedRegex(@"(?<=[.!?…])\s+")]
	private static partial Regex SentenceSplitRegex();
}
