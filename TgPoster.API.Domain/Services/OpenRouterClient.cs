using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TgPoster.API.Domain.Exceptions;

namespace TgPoster.API.Domain.Services;

public class OpenRouterClient(IHttpClientFactory httpClientFactory)
{
	private const string ApiUrl = "https://openrouter.ai/api/v1/chat/completions";

	public async Task<ChatCompletionResponse?> SendMessageAsync(string apiKey, string message, string model)
	{
		using var client = httpClientFactory.CreateClient();
		client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

		var requestData = new ChatRequest
		{
			Model = model,
			Messages =
			[
				new ChatMessage
				{
					Content = message,
					Role = "assistant"
				}
			]
		};

		var response = await client.PostAsync(ApiUrl, requestData.ToStringContent());
		if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
			throw new OpenRouterNotAuthorizedException();

		var responseBody = await response.Content.ReadAsStringAsync();

		if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
		{
			var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody);
			throw new OpenRouterException(errorResponse?.Error.Message);
		}

		var chatResponse = JsonSerializer.Deserialize<ChatCompletionResponse>(responseBody);
		return chatResponse;
	}
}

public static class ContentExtension
{
	public static StringContent ToStringContent(this object obj)
	{
		var json = JsonSerializer.Serialize(obj);
		return new StringContent(json, Encoding.UTF8, "application/json");
	}
}

/// <summary>
/// Представляет тело запроса на чат.
/// </summary>
public class ChatRequest
{
	[JsonPropertyName("model")]
	public string Model { get; set; }

	[JsonPropertyName("messages")]
	public List<ChatMessage> Messages { get; set; }

	// Сюда можно добавить другие параметры API, например:
	// [JsonPropertyName("temperature")]
	// public double? Temperature { get; set; }
}

/// <summary>
/// Представляет одно сообщение в диалоге (как в запросе, так и в ответе).
/// </summary>
public class ChatMessage
{
	[JsonPropertyName("role")]
	public string Role { get; set; }

	[JsonPropertyName("content")]
	public string Content { get; set; }
}

// --- МОДЕЛИ ДЛЯ ОТВЕТА ---

/// <summary>
/// Представляет полный объект ответа от API.
/// </summary>
public class ChatCompletionResponse
{
	[JsonPropertyName("id")]
	public string Id { get; set; }

	[JsonPropertyName("model")]
	public string Model { get; set; }

	[JsonPropertyName("choices")]
	public List<Choice> Choices { get; set; }

	[JsonPropertyName("usage")]
	public UsageStats Usage { get; set; }
}

/// <summary>
/// Представляет один из вариантов ответа, сгенерированных моделью.
/// </summary>
public class Choice
{
	[JsonPropertyName("message")]
	public ChatMessage Message { get; set; }

	[JsonPropertyName("finish_reason")]
	public string FinishReason { get; set; }
}

/// <summary>
/// Представляет статистику по использованию токенов.
/// </summary>
public class UsageStats
{
	[JsonPropertyName("prompt_tokens")]
	public int PromptTokens { get; set; }

	[JsonPropertyName("completion_tokens")]
	public int CompletionTokens { get; set; }

	[JsonPropertyName("total_tokens")]
	public int TotalTokens { get; set; }
}

public class ErrorResponse
{
	[JsonPropertyName("error")]
	public ErrorDetails Error { get; set; }

	[JsonPropertyName("user_id")]
	public string UserId { get; set; }
}

public class ErrorDetails
{
	[JsonPropertyName("message")]
	public string Message { get; set; }

	[JsonPropertyName("code")]
	public int Code { get; set; }
}