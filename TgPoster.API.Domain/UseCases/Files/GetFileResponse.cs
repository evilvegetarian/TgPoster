namespace TgPoster.API.Domain.UseCases.Files;

public record GetFileResponse(byte[] Data, string ContentType, string FileName);