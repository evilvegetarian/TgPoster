using TgPoster.Storage.Data.Enum;

namespace TgPoster.Storage.Data.Entities;

public sealed class MessageFile : BaseEntity
{
	/// <summary>
	///     Id сообщения
	/// </summary>
	public required Guid MessageId { get; set; }

	/// <summary>
	///     Тип файла (Photo, Video, Thumbnail)
	/// </summary>
	public required FileTypes FileType { get; set; }

	/// <summary>
	///     Id файла в телеграме
	/// </summary>
	public required string TgFileId { get; set; }

	/// <summary>
	///     Тип контента (image/jpeg, video/mp4)
	/// </summary>
	public required string ContentType { get; set; }

	/// <summary>
	///     Подпись к файлу (используется для caption в Telegram)
	/// </summary>
	public string? Caption { get; set; }

	/// <summary>
	///     Id родительского файла (для превью указывает на видео, для основных файлов = null)
	/// </summary>
	public Guid? ParentFileId { get; set; }

	/// <summary>
	///     Порядок отображения файла в сообщении (0 - первый файл и т.д.)
	/// </summary>
	public int Order { get; set; }

	/// <summary>
	///     Признак того, что файл загружен в S3
	/// </summary>
	public bool IsInS3 { get; set; }

	/// <summary>
	///     Длительность видео (только для видео файлов)
	/// </summary>
	public TimeSpan? Duration { get; set; }

	#region Навигация

	/// <summary>
	///     Сообщение
	/// </summary>
	public Message Message { get; set; } = null!;

	/// <summary>
	///     Родительский файл (например, видео для превью)
	/// </summary>
	public MessageFile? ParentFile { get; set; }

	/// <summary>
	///     Дочерние файлы (превью для видео)
	/// </summary>
	public ICollection<MessageFile> Thumbnails { get; set; } = [];

	#endregion
}