using System.Text.Json.Serialization;

namespace Shared.OpenRouter.Models.Response;

/// <summary>
/// Представляет статистику по использованию токенов.
/// </summary>
public sealed class UsageStats
{
	[JsonPropertyName("prompt_tokens")]
	public int PromptTokens { get; set; }

	[JsonPropertyName("completion_tokens")]
	public int CompletionTokens { get; set; }

	[JsonPropertyName("total_tokens")]
	public int TotalTokens { get; set; }
}
