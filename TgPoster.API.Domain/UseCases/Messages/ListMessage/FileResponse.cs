using Shared;

namespace TgPoster.API.Domain.UseCases.Messages.ListMessage;

public class FileResponse
{
	/// <summary>
	///     Id файла
	/// </summary>
	public required Guid Id { get; set; }

	/// <summary>
	///     Тип файла.
	/// </summary>
	public FileTypes FileType { get; set; }

	/// <summary>
	///     Url картинки
	/// </summary>
	public string? Url { get; set; }

	public List<PreviewFileResponse> PreviewFiles { get; set; } = [];
}

public sealed class PreviewFileResponse
{
	public string Url { get; set; }
}