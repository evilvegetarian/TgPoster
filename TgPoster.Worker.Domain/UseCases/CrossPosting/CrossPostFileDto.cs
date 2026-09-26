namespace TgPoster.Worker.Domain.UseCases.CrossPosting;

/// <summary>
///     Файл сообщения для кросс-постинга
/// </summary>
public sealed class CrossPostFileDto
{
	/// <summary>
	///     Идентификатор файла в Telegram
	/// </summary>
	public required string TgFileId { get; init; }

	/// <summary>
	///     MIME-тип файла
	/// </summary>
	public required string ContentType { get; init; }

	/// <summary>
	///     Порядковый номер файла в сообщении
	/// </summary>
	public int Order { get; init; }

	/// <summary>
	///     Идентификатор превью видео в Telegram; для фото — null
	/// </summary>
	public string? ThumbnailTgFileId { get; init; }
}
