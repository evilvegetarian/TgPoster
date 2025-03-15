using Microsoft.AspNetCore.Http;

namespace TgPoster.Domain.Services;

public static class ContentTypeExtensions
{
    public static FileTypes GetFileType(this IFormFile file)
    {
        return file.ContentType.GetFileType();
    }

    public static FileTypes GetFileType(this string contentType)
    {
        if (contentType.StartsWith("image"))
            return FileTypes.Photo;
        if (contentType.StartsWith("video"))
            return FileTypes.Video;
        return FileTypes.NoOne;
    }
}