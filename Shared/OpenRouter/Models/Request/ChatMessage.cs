using System.Text.Json.Serialization;

namespace Shared.OpenRouter.Models.Request;

/// <summary>
///     Представляет одно сообщение в чате.
/// </summary>
public sealed class ChatMessage
{
	[JsonPropertyName("role")]
	public required string Role { get; set; }

	[JsonPropertyName("content")]
	public required object Content { get; set; }
}