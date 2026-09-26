namespace Shared.CrossPosting;

/// <summary>
///     Способ подсчёта длины текста для площадки
/// </summary>
public enum TextLengthMode
{
	/// <summary>
	///     Количество UTF-16 символов
	/// </summary>
	Utf16,

	/// <summary>
	///     Количество текстовых элементов (графем)
	/// </summary>
	Graphemes,

	/// <summary>
	///     Количество символов по правилам Threads
	/// </summary>
	ThreadsChars
}
