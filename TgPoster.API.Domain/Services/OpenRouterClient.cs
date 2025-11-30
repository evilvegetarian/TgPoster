using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TgPoster.API.Domain.Exceptions;

namespace TgPoster.API.Domain.Services;

public class OpenRouterClient(IHttpClientFactory httpClientFactory)
{
	private const string ApiUrl = "https://openrouter.ai/api/v1/chat/completions";

	public async Task<ChatCompletionResponse?> SendMessageAsync(string apiKey, string model, string message)
	{
		return await SendMessageRawAsync(apiKey, model,
			[
				new ChatMessage { Role = "user", Content = message }
			]
		);
	}

	public async Task<ChatCompletionResponse?> SendImageMessageAsync(
		string apiKey,
		string model,
		string textPrompt,
		string imageUrl
	)
	{
		var contentParts = new List<MessageContentPart>
		{
			new() { Type = "text", Text = textPrompt },
			new()
			{
				Type = "image_url",
				ImageUrl = new ImageUrlInfo { Url = imageUrl }
			}
		};

		var message = new ChatMessage
		{
			Role = "user",
			Content = contentParts
		};

		return await SendMessageRawAsync(apiKey, model, [message]);
	}

	public async Task<ChatCompletionResponse?> SendMessageRawAsync(
		string apiKey,
		string model,
		List<ChatMessage> messages
	)
	{
		using var client = httpClientFactory.CreateClient();
		client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

		var requestData = new ChatRequest
		{
			Model = model,
			Messages = messages
		};

		var response = await client.PostAsync(ApiUrl, requestData.ToStringContent());

		var responseBody = await response.Content.ReadAsStringAsync();

		if (!response.IsSuccessStatusCode)
		{
			if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
				throw new OpenRouterNotAuthorizedException();

			try
			{
				var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody);
				throw new OpenRouterException(errorResponse?.Error.Message ?? responseBody);
			}
			catch (JsonException)
			{
				throw new OpenRouterException($"API Error {response.StatusCode}: {responseBody}");
			}
		}

		return JsonSerializer.Deserialize<ChatCompletionResponse>(responseBody);
	}

	public string ToLocalImageExample(byte[] picture)
	{
		//var imageBytes = stream.ToArray();
		var base64Image = Convert.ToBase64String(picture);

		return $"data:image/jpeg;base64,{base64Image}";
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
/// Представляет одно сообщение.
/// </summary>
public class ChatMessage
{
	[JsonPropertyName("role")]
	public string Role { get; set; }

	// ИЗМЕНЕНИЕ: Тип string заменен на object, чтобы поддерживать и строку, и список
	[JsonPropertyName("content")]
	public object Content { get; set; }
}

// --- НОВЫЕ КЛАССЫ ДЛЯ КАРТИНОК ---

public class MessageContentPart
{
	[JsonPropertyName("type")]
	public string Type { get; set; } // "text" или "image_url"

	[JsonPropertyName("text")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Text { get; set; }

	[JsonPropertyName("image_url")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public ImageUrlInfo ImageUrl { get; set; }
}

public class ImageUrlInfo
{
	[JsonPropertyName("url")]
	public string Url { get; set; }

	// Опционально: "auto", "low", "high"
	[JsonPropertyName("detail")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string Detail { get; set; }
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