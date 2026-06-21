namespace TgPoster.Worker.Domain.UseCases.CleanupS3Files;

public interface ICleanupS3FilesStorage
{
	/// <summary>
	///     Сбрасывает признак нахождения файлов в S3 для указанных идентификаторов
	/// </summary>
	/// <param name="fileIds">Идентификаторы файлов, удалённых из S3.</param>
	/// <param name="ct">Токен отмены.</param>
	Task ResetIsInS3FlagAsync(IReadOnlyCollection<Guid> fileIds, CancellationToken ct);
}