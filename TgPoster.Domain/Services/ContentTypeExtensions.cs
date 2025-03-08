using Microsoft.AspNetCore.Http;

namespace TgPoster.Domain.Services;


public static class ContentTypeExtensions
{
    //TODO: Это не ContentType нужно переделать на что то типа FileType
    public static ContentTypes GetContentType(this IFormFile file)
    {
        if (file.ContentType.StartsWith("image"))
            return ContentTypes.Photo;
        else if (file.ContentType.StartsWith("video"))
            return ContentTypes.Video;
        return ContentTypes.NoOne;
    }
}