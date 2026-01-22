namespace Shared.Utilities;

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
		return type switch
		{
			FileTypes.NoOne => "NoOne",
			FileTypes.Image => "Фото",
			FileTypes.Video => "Видео",
			_ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
		};
	}

	public static string GetContentType(this FileTypes type)
	{
		return type switch
		{
			FileTypes.Image => "image/jpeg",
			FileTypes.Video => "video/mp4",
			_ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
		};
	}
}