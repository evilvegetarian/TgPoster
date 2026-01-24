namespace TgPoster.API.Domain.UseCases.Files.GetFile;

public sealed record GetFileResponse(byte[] Data, string ContentType, string FileName);