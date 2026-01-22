using System.Text.Json.Serialization;
using Shared.OpenRouter.Models.Request;

namespace Shared.OpenRouter.Models.Response;

/// <summary>
///     Представляет один из вариантов ответа, сгенерированных моделью.
/// </summary>
public sealed class Choice
{
	[JsonPropertyName("message")]
	public required ChatMessage Message { get; set; }

	[JsonPropertyName("finish_reason")]
	public required string FinishReason { get; set; }
}