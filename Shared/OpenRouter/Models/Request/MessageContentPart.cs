using System.Text.Json.Serialization;

namespace Shared.OpenRouter.Models.Request;

/// <summary>
///     Представляет часть контента сообщения (текст или изображение).
/// </summary>
public sealed class MessageContentPart
{
	[JsonPropertyName("type")]
	public required string Type { get; set; }

	[JsonPropertyName("text")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Text { get; set; }

	[JsonPropertyName("image_url")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public ImageUrlInfo? ImageUrl { get; set; }
}