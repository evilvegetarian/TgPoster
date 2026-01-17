namespace TgPoster.API.Domain.Services;

/// <summary>
///     Интерфейс для работы с хранилищем файлов
/// </summary>
public interface IFileStorage
{
	/// <summary>
	///     Установить признак того, что файл загружен в S3
	/// </summary>
	/// <param name="fileId">Идентификатор файла</param>
	/// <param name="ct">Токен отмены операции</param>
	Task MarkFileAsUploadedToS3Async(Guid fileId, CancellationToken ct);
}
