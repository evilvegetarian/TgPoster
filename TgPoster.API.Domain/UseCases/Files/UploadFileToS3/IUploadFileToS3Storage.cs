namespace TgPoster.API.Domain.UseCases.Files.UploadFileToS3;

/// <summary>
///     Интерфейс для работы с хранилищем при загрузке файлов в S3
/// </summary>
public interface IUploadFileToS3Storage
{
	/// <summary>
	///     Получить информацию о файле для загрузки в S3
	/// </summary>
	/// <param name="fileId">Идентификатор файла</param>
	/// <param name="ct">Токен отмены операции</param>
	/// <returns>Информация о файле или null, если файл не найден</returns>
	Task<FileInfoDto?> GetFileInfoAsync(Guid fileId, CancellationToken ct);

	/// <summary>
	///     Установить признак того, что файл загружен в S3
	/// </summary>
	/// <param name="fileId">Идентификатор файла</param>
	/// <param name="ct">Токен отмены операции</param>
	Task MarkFileAsUploadedToS3Async(Guid fileId, CancellationToken ct);

	/// <summary>
	///     Получить идентификатор расписания по идентификатору файла
	/// </summary>
	/// <param name="fileId">Идентификатор файла</param>
	/// <param name="ct">Токен отмены операции</param>
	/// <returns>Идентификатор расписания</returns>
	Task<Guid> GetScheduleIdByFileIdAsync(Guid fileId, CancellationToken ct);
}

/// <summary>
///     DTO с информацией о файле
/// </summary>
public sealed class FileInfoDto
{
	public required string TgFileId { get; set; }
	public required string ContentType { get; set; }
	public bool IsInS3 { get; set; }
}