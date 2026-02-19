using Shared.Utilities;

namespace TgPoster.API.Domain.UseCases.Messages.ListMessage;

public sealed record FileResponse
{
	/// <summary>
	///     Id файла
	/// </summary>
	public required Guid Id { get; init; }

	/// <summary>
	///     Тип файла.
	/// </summary>
	public FileTypes FileType { get; init; }

	/// <summary>
	///     Url картинки
	/// </summary>
	public string? Url { get; init; }

	/// <summary>
	///     URL видео для воспроизведения (полное видео или обрезанный клип)
	/// </summary>
	public string? VideoUrl { get; init; }

	/// <summary>
	///     Длительность оригинального видео в секундах
	/// </summary>
	public double? DurationSeconds { get; init; }

	public List<PreviewFileResponse> PreviewFiles { get; init; } = [];
}

public sealed record PreviewFileResponse
{
	public required string Url { get; init; }
}