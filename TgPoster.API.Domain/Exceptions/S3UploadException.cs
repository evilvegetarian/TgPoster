using System.Net;

namespace TgPoster.API.Domain.Exceptions;

/// <summary>
///     Исключение, выбрасываемое при ошибке загрузки файла в S3.
/// </summary>
public sealed class S3UploadException : DomainException
{
	public Guid FileId { get; }
	public HttpStatusCode StatusCode { get; }

	public S3UploadException(Guid fileId, HttpStatusCode statusCode)
		: base($"Не удалось загрузить файл {fileId} в S3. Статус: {statusCode}")
	{
		FileId = fileId;
		StatusCode = statusCode;
	}
}
