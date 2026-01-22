using System.Text.Json.Serialization;

namespace Shared.OpenRouter.Models.Request;

/// <summary>
///     Представляет информацию об URL изображения.
/// </summary>
public sealed class ImageUrlInfo
{
	[JsonPropertyName("url")]
	public required string Url { get; set; }

	[JsonPropertyName("detail")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Detail { get; set; }
}