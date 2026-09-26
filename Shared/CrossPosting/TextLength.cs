using System.Globalization;
using System.Text;

namespace Shared.CrossPosting;

/// <summary>
///     Подсчёт длины текста по правилам площадок
/// </summary>
public static class TextLength
{
	/// <summary>
	///     Измерить длину текста в выбранном режиме
	/// </summary>
	/// <param name="text"></param>
	/// <param name="mode"></param>
	/// <returns></returns>
	public static int Measure(string text, TextLengthMode mode)
	{
		return mode switch
		{
			TextLengthMode.Utf16 => text.Length,
			TextLengthMode.Graphemes => new StringInfo(text).LengthInTextElements,
			TextLengthMode.ThreadsChars => MeasureThreadsChars(text),
			_ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
		};
	}

	/// <summary>
	///     Разбить текст на текстовые элементы
	/// </summary>
	/// <param name="text"></param>
	/// <returns></returns>
	public static IReadOnlyList<string> SplitTextElements(string text)
	{
		var elements = new List<string>();
		var enumerator = StringInfo.GetTextElementEnumerator(text);
		while (enumerator.MoveNext())
		{
			elements.Add((string)enumerator.Current);
		}

		return elements;
	}

	/// <summary>
	///     Измерить длину текста по правилам Threads
	/// </summary>
	/// <param name="text"></param>
	/// <returns></returns>
	private static int MeasureThreadsChars(string text)
	{
		var total = 0;
		var enumerator = StringInfo.GetTextElementEnumerator(text);
		while (enumerator.MoveNext())
		{
			var element = (string)enumerator.Current;
			total += element.Length == 1 && !char.IsSurrogate(element[0])
				? 1
				: Encoding.UTF8.GetByteCount(element);
		}

		return total;
	}
}
