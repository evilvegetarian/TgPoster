namespace Shared.Exceptions;

/// <summary>
///     Исключение, выбрасываемое когда видеопоток null или некорректный.
/// </summary>
public sealed class InvalidVideoStreamException()
	: SharedException("Видеопоток не может быть null");