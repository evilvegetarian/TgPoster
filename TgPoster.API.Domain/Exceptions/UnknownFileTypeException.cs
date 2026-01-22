namespace TgPoster.API.Domain.Exceptions;

/// <summary>
///     Исключение, выбрасываемое когда тип файла неизвестен.
/// </summary>
public sealed class UnknownFileTypeException(string fileType)
	: DomainException($"Неизвестный тип контента: {fileType}");