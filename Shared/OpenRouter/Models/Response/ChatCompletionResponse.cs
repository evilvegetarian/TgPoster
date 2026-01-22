using System.Text.Json.Serialization;

namespace Shared.OpenRouter.Models.Response;

/// <summary>
/// Представляет полный объект ответа от API.
/// </summary>
public sealed class ChatCompletionResponse
{
	[JsonPropertyName("id")]
	public required string Id { get; set; }

	[JsonPropertyName("model")]
	public required string Model { get; set; }

	[JsonPropertyName("choices")]
	public required List<Choice> Choices { get; set; }

	[JsonPropertyName("usage")]
	public required UsageStats Usage { get; set; }
}
