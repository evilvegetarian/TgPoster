namespace Shared;

public enum FileTypes
{
    NoOne = 0,
    Image = 1,
    Video = 2
}

public static class FileTypesExtensions
{
    public static string GetName(this FileTypes type)
    {
        switch (type)
        {
            case FileTypes.NoOne:
                return "NoOne";
            case FileTypes.Image:
                return "Фото";
            case FileTypes.Video:
                return "Видео";
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }
}