
namespace TgPoster.Domain.Services;

public enum ContentTypes
{
    NoOne = 0,
    Photo = 1,
    Video = 2
}

public static class ContentTypesExtentions
{
    public static string GetMimeType(this ContentTypes types)
    {
        switch (types)
        {
            case ContentTypes.NoOne:
                break;
            case ContentTypes.Photo:
                return "image/jpeg";
            case ContentTypes.Video:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(types), types, null);
        }

        return null;
    }
}

public static class MimeType
{
    public static string Photo => "image/jpeg";
}