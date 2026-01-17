using MediatR;

namespace TgPoster.API.Domain.UseCases.Files.UploadFileToS3;

/// <summary>
///     Команда для загрузки файла в S3
/// </summary>
/// <param name="FileId">Идентификатор файла</param>
public sealed record UploadFileToS3Command(Guid FileId) : IRequest<UploadFileToS3Response>;