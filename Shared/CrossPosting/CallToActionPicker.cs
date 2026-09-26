using Shared.Enums;

namespace Shared.CrossPosting;

/// <summary>
///     Выбор призыва к действию для кросс-поста
/// </summary>
public static class CallToActionPicker
{
	/// <summary>
	///     Получить стандартный призыв для формата
	/// </summary>
	/// <param name="format"></param>
	/// <returns></returns>
	public static string Default(CrossPostFormat format)
	{
		return format switch
		{
			CrossPostFormat.Teaser => "Читать полностью в Telegram 👇",
			CrossPostFormat.Full => "Больше интересного — в нашем Telegram 👇",
			CrossPostFormat.Announcement => "Новый пост в нашем Telegram-канале 👇",
			_ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
		};
	}

	/// <summary>
	///     Выбрать случайный вариант призыва
	/// </summary>
	/// <param name="variants"></param>
	/// <param name="format"></param>
	/// <param name="random"></param>
	/// <returns></returns>
	public static string Pick(string? variants, CrossPostFormat format, Random random)
	{
		var list = ParseVariants(variants);
		if (list.Count == 0)
		{
			return Default(format);
		}

		if (list.Count == 1)
		{
			return list[0];
		}

		return list[random.Next(list.Count)];
	}

	/// <summary>
	///     Взять первый вариант призыва
	/// </summary>
	/// <param name="variants"></param>
	/// <param name="format"></param>
	/// <returns></returns>
	public static string First(string? variants, CrossPostFormat format)
	{
		var list = ParseVariants(variants);
		return list.Count > 0 ? list[0] : Default(format);
	}

	private static IReadOnlyList<string> ParseVariants(string? variants)
	{
		if (string.IsNullOrWhiteSpace(variants))
		{
			return [];
		}

		var lines = variants.Split('\n');
		var result = new List<string>(lines.Length);
		foreach (var line in lines)
		{
			var trimmed = line.Trim();
			if (trimmed.Length > 0)
			{
				result.Add(trimmed);
			}
		}

		return result;
	}
}
