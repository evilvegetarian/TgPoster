namespace TgPoster.Storage.Data.Entities;

public sealed class FileThumbnail : BaseEntity
{
	/// <summary>
	///     Id файла в телеграме
	/// </summary>
	public required string TgFileId { get; set; }

	/// <summary>
	///     Ссылка на родительский файл (видео)
	/// </summary>
	public required Guid MessageFileId { get; set; }

	/// <summary>
	///     Тип файла
	/// </summary>
	public required string ContentType { get; set; }

	#region Navigation

	public MessageFile MessageFile { get; set; } = null!;

	#endregion
}