namespace TgPoster.Storage.Data.Enum;

public enum FileTypes
{
	NoOne = 0,
	Photo = 1,
	Video = 2,
	Thumbnail = 3,
	VideoClip = 4
}

public static class FileTypesExtensions
{
	public static FileTypes GetFileType(this string contentType)
	{
		if (contentType.StartsWith("image"))
		{
			return FileTypes.Photo;
		}

		if (contentType.StartsWith("video"))
		{
			return FileTypes.Video;
		}

		return FileTypes.NoOne;
	}

	public static string GetContentType(this FileTypes type)
	{
		return type switch
		{
			FileTypes.Photo => "image/jpeg",
			FileTypes.Thumbnail => "image/jpeg",
			FileTypes.Video => "video/mp4",
			FileTypes.VideoClip => "video/mp4",
			_ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
		};
	}
}