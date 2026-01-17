namespace TgPoster.API.Domain.UseCases.Files.UploadFileToS3;

/// <summary>
///     Ответ на команду загрузки файла в S3
/// </summary>
/// <param name="S3Url">URL файла в S3</param>
public sealed record UploadFileToS3Response(string S3Url);