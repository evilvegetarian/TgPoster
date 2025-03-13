namespace TgPoster.Domain.UseCases.Files;

public record GetFileResponse(byte[] Data, string ContentType, string FileName);