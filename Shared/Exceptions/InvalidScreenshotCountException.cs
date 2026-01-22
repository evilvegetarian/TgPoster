namespace Shared.Exceptions;

/// <summary>
///     Исключение, выбрасываемое когда количество скриншотов некорректное.
/// </summary>
public sealed class InvalidScreenshotCountException(int count)
	: SharedException($"Количество скриншотов должно быть не меньше 1. Указано: {count}");
