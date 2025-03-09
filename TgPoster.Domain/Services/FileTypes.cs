namespace TgPoster.Domain.Services;

public enum FileTypes
{
    NoOne = 0,
    Photo = 1,
    Video = 2
}

public static class ContentTypesExtentions
{
    public static string GetMimeType(this FileTypes types)
    {
        switch (types)
        {
            case FileTypes.NoOne:
                break;
            case FileTypes.Photo:
                return "image/jpeg";
            case FileTypes.Video:
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