using System.Text.Json.Serialization;

namespace Shared.OpenRouter.Models.Response;

/// <summary>
/// Представляет ответ об ошибке от API.
/// </summary>
public sealed class ErrorResponse
{
	[JsonPropertyName("error")]
	public required ErrorDetails Error { get; set; }

	[JsonPropertyName("user_id")]
	public string? UserId { get; set; }
}
