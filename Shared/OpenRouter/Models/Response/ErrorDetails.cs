using System.Text.Json.Serialization;

namespace Shared.OpenRouter.Models.Response;

/// <summary>
/// Представляет детали ошибки от API.
/// </summary>
public sealed class ErrorDetails
{
	[JsonPropertyName("message")]
	public required string Message { get; set; }

	[JsonPropertyName("code")]
	public int Code { get; set; }
}
