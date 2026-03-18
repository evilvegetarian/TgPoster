using System.Text.Json.Serialization;

namespace TgPoster.Worker.Domain.UseCases.ClassifyChannel;

/// <summary>
///     Результат классификации канала от LLM.
/// </summary>
internal sealed class ChannelClassificationResult
{
	[JsonPropertyName("category")]
	public string? Category { get; set; }

	[JsonPropertyName("subcategory")]
	public string? Subcategory { get; set; }

	[JsonPropertyName("tags")]
	public string[]? Tags { get; set; }

	[JsonPropertyName("language")]
	public string? Language { get; set; }

	[JsonPropertyName("confidence")]
	public double Confidence { get; set; }
}
