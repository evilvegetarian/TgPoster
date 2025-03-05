namespace TgPoster.Domain.UseCases.Files;

public record FileResponse(byte[] Data, string ContentType, string FileName);