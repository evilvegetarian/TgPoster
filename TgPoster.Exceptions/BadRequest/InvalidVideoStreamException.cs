using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.BadRequest;

/// <summary>
///     Исключение, выбрасываемое когда видеопоток null или некорректный
/// </summary>
public sealed class InvalidVideoStreamException()
	: DomainException("Видеопоток не может быть null");