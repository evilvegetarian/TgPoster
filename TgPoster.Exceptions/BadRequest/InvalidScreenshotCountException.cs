namespace TgPoster.Exceptions;

/// <summary>
///     Исключение, выбрасываемое когда количество скриншотов некорректное
/// </summary>
/// <param name="count">Указанное количество скриншотов</param>
public sealed class InvalidScreenshotCountException(int count)
	: DomainException($"Количество скриншотов должно быть не меньше 1. Указано: {count}");
