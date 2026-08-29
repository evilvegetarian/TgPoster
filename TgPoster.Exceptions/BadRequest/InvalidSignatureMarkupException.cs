using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.BadRequest;

/// <summary>
///     Исключение, выбрасываемое когда подпись расписания содержит неподдерживаемую Telegram HTML-разметку
/// </summary>
public sealed class InvalidSignatureMarkupException()
	: DomainException(
		"Подпись содержит некорректную HTML-разметку. Допустимы только теги b, strong, i, em, u, ins, s, strike, del, a, code, pre, span, tg-spoiler, blockquote, и все они должны быть закрыты");
