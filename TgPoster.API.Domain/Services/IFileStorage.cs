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

	/// <summary>
	///     Получить информацию о файле из БД
	/// </summary>
	/// <param name="fileId">Идентификатор файла</param>
	/// <param name="ct">Токен отмены операции</param>
	/// <returns>Информация о файле или null, если файл не найден</returns>
	Task<MessageFileInfo?> GetFileInfoAsync(Guid fileId, CancellationToken ct);
}

/// <summary>
///     Информация о файле из БД
/// </summary>
public sealed class MessageFileInfo
{
	/// <summary>
	///     Идентификатор файла в Telegram
	/// </summary>
	public required string TgFileId { get; init; }

	/// <summary>
	///     Тип контента (MIME-type)
	/// </summary>
	public required string ContentType { get; init; }

	/// <summary>
	///     Идентификатор сообщения
	/// </summary>
	public required Guid MessageId { get; init; }
}