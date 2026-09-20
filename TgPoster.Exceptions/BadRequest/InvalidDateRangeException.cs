using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.BadRequest;

/// <summary>
///     Исключение, выбрасываемое когда начало периода позже его конца
/// </summary>
/// <param name="from">Начало периода</param>
/// <param name="to">Конец периода</param>
public sealed class InvalidDateRangeException(DateTimeOffset from, DateTimeOffset to)
	: DomainException($"Начало периода {from:O} не может быть позже его конца {to:O}");
