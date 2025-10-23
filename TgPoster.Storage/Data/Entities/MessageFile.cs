namespace TgPoster.Storage.Data.Entities;

public class MessageFile : BaseEntity
{
	/// <summary>
	///     Id сообщения
	/// </summary>
	public required Guid MessageId { get; set; }

	/// <summary>
	///     Id файла в телеграме
	/// </summary>
	public required string TgFileId { get; set; }

	/// <summary>
	///     Подпись к фото
	/// </summary>
	public string? Caption { get; set; }

	/// <summary>
	///     Тип файла
	/// </summary>
	public required string ContentType { get; set; }

	/// <summary>
	///     Сообщение
	/// </summary>
	public Message Message { get; set; } = null!;
}