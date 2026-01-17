namespace TgPoster.API.Domain.UseCases.Files.GetFile;

public record GetFileResponse(byte[] Data, string ContentType, string FileName);