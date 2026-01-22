using System.Text.Json.Serialization;

namespace Shared.OpenRouter.Models.Request;

/// <summary>
///     Представляет тело запроса на чат.
/// </summary>
public sealed class ChatRequest
{
	[JsonPropertyName("model")]
	public required string Model { get; set; }

	[JsonPropertyName("messages")]
	public required List<ChatMessage> Messages { get; set; }
}