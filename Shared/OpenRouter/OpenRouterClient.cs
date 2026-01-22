using System.Net;
using System.Text.Json;
using Shared.Exceptions;
using Shared.OpenRouter.Extensions;
using Shared.OpenRouter.Models.Request;
using Shared.OpenRouter.Models.Response;

namespace Shared.OpenRouter;

/// <summary>
///     Клиент для работы с OpenRouter API.
/// </summary>
public sealed class OpenRouterClient(IHttpClientFactory httpClientFactory) : IOpenRouterClient
{
	private const string ApiUrl = "https://openrouter.ai/api/v1/chat/completions";

	/// <inheritdoc />
	public Task<ChatCompletionResponse> SendMessageAsync(
		string apiKey,
		string model,
		string message,
		CancellationToken cancellationToken
	)
	{
		var messages = new List<ChatMessage>
		{
			new() { Role = "user", Content = message }
		};

		return SendMessageRawAsync(apiKey, model, messages, cancellationToken);
	}

	/// <inheritdoc />
	public Task<ChatCompletionResponse> SendImageMessageAsync(
		string apiKey,
		string model,
		string textPrompt,
		string imageUrl,
		CancellationToken cancellationToken
	)
	{
		var contentParts = new List<MessageContentPart>
		{
			new() { Type = "text", Text = textPrompt },
			new() { Type = "image_url", ImageUrl = new ImageUrlInfo { Url = imageUrl } }
		};

		var message = new ChatMessage { Role = "user", Content = contentParts };
		var messages = new List<ChatMessage> { message };

		return SendMessageRawAsync(apiKey, model, messages, cancellationToken);
	}

	/// <inheritdoc />
	public async Task<ChatCompletionResponse> SendMessageRawAsync(
		string apiKey,
		string model,
		List<ChatMessage> messages,
		CancellationToken cancellationToken
	)
	{
		using var client = httpClientFactory.CreateClient();
		client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

		var requestData = new ChatRequest { Model = model, Messages = messages };

		var response = await client.PostAsync(
			ApiUrl,
			requestData.ToJsonStringContent(),
			cancellationToken);

		var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

		if (!response.IsSuccessStatusCode)
		{
			HandleErrorResponse(response.StatusCode, responseBody);
		}

		var result = JsonSerializer.Deserialize<ChatCompletionResponse>(responseBody);
		if (result is null)
		{
			throw new OpenRouterException("Не удалось десериализовать ответ от API");
		}

		return result;
	}

	/// <inheritdoc />
	public string ToLocalImageDataUrl(byte[] picture)
	{
		var base64Image = Convert.ToBase64String(picture);
		return $"data:image/jpeg;base64,{base64Image}";
	}

	private static void HandleErrorResponse(HttpStatusCode statusCode, string responseBody)
	{
		if (statusCode == HttpStatusCode.Unauthorized)
		{
			throw new OpenRouterNotAuthorizedException();
		}

		try
		{
			var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody);
			var errorMessage = errorResponse?.Error.Message ?? responseBody;
			throw new OpenRouterException(errorMessage);
		}
		catch (JsonException)
		{
			throw new OpenRouterException($"API Error {statusCode}: {responseBody}");
		}
	}
}