using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.BadRequest;

/// <summary>
///     Исключение, выбрасываемое когда тип файла неизвестен
/// </summary>
/// <param name="fileType">Имя неизвестного типа</param>
public sealed class UnknownFileTypeException(string fileType)
	: DomainException($"Неизвестный тип контента: {fileType}");
