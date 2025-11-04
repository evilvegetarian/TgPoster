namespace TgPoster.Storage.Data.Entities;

public sealed class VideoMessageFile : MessageFile
{
	/// <summary>
	///     Превью видео
	/// </summary>
	public ICollection<string> ThumbnailIds { get; set; } = [];
}