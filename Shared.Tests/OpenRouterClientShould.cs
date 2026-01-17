using System.Net;
using System.Text.Json;
using Moq;
using Moq.Protected;
using Shared;
using Shared.SharedException;
using Shouldly;

namespace Shared.Tests;

/// <summary>
/// Тесты для класса OpenRouterClient
/// </summary>
public sealed class OpenRouterClientShould
{
	private readonly Mock<IHttpClientFactory> httpClientFactoryMock;
	private readonly Mock<HttpMessageHandler> httpMessageHandlerMock;

	public OpenRouterClientShould()
	{
		httpMessageHandlerMock = new Mock<HttpMessageHandler>();
		httpClientFactoryMock = new Mock<IHttpClientFactory>();

		var httpClient = new HttpClient(httpMessageHandlerMock.Object);
		httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
	}

	[Fact]
	public async Task SendTextMessageSuccessfully()
	{
		var jsonResponse = """
		{
			"id": "test-id",
			"model": "test-model",
			"choices": [
				{
					"message": {
						"role": "assistant",
						"content": "Test response"
					},
					"finish_reason": "stop"
				}
			],
			"usage": {
				"prompt_tokens": 10,
				"completion_tokens": 20,
				"total_tokens": 30
			}
		}
		""";

		httpMessageHandlerMock
			.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>()
			)
			.ReturnsAsync(new HttpResponseMessage
			{
				StatusCode = HttpStatusCode.OK,
				Content = new StringContent(jsonResponse)
			});

		var client = new OpenRouterClient(httpClientFactoryMock.Object);

		var result = await client.SendMessageAsync("test-api-key", "gpt-4", "Hello world");

		result.ShouldNotBeNull();
		result.Id.ShouldBe("test-id");
		result.Choices.Count.ShouldBe(1);
		result.Choices[0].Message.Content.ToString().ShouldBe("Test response");
	}

	[Fact]
	public async Task SendImageMessageSuccessfully()
	{
		var jsonResponse = """
		{
			"id": "test-id-image",
			"model": "test-model",
			"choices": [
				{
					"message": {
						"role": "assistant",
						"content": "Image processed"
					},
					"finish_reason": "stop"
				}
			],
			"usage": {
				"prompt_tokens": 100,
				"completion_tokens": 50,
				"total_tokens": 150
			}
		}
		""";

		httpMessageHandlerMock
			.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>()
			)
			.ReturnsAsync(new HttpResponseMessage
			{
				StatusCode = HttpStatusCode.OK,
				Content = new StringContent(jsonResponse)
			});

		var client = new OpenRouterClient(httpClientFactoryMock.Object);

		var result = await client.SendImageMessageAsync("test-api-key", "gpt-4-vision", "Analyze this image", "https://example.com/image.jpg");

		result.ShouldNotBeNull();
		result.Choices[0].Message.Content.ToString().ShouldBe("Image processed");
	}

	[Fact]
	public async Task SendRawMessageSuccessfully()
	{
		var jsonResponse = """
		{
			"id": "test-id-raw",
			"model": "test-model",
			"choices": [
				{
					"message": {
						"role": "assistant",
						"content": "Response to multiple messages"
					},
					"finish_reason": "stop"
				}
			],
			"usage": {
				"prompt_tokens": 50,
				"completion_tokens": 30,
				"total_tokens": 80
			}
		}
		""";

		httpMessageHandlerMock
			.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>()
			)
			.ReturnsAsync(new HttpResponseMessage
			{
				StatusCode = HttpStatusCode.OK,
				Content = new StringContent(jsonResponse)
			});

		var client = new OpenRouterClient(httpClientFactoryMock.Object);
		var messages = new List<ChatMessage>
		{
			new() { Role = "system", Content = "You are a helpful assistant" },
			new() { Role = "user", Content = "Hello" }
		};

		var result = await client.SendMessageRawAsync("test-api-key", "gpt-4", messages);

		result.ShouldNotBeNull();
		result.Choices[0].Message.Content.ToString().ShouldBe("Response to multiple messages");
	}

	[Fact]
	public async Task ThrowOpenRouterNotAuthorizedExceptionOnUnauthorized()
	{
		SetupHttpResponse(HttpStatusCode.Unauthorized, "Unauthorized");

		var client = new OpenRouterClient(httpClientFactoryMock.Object);

		await Should.ThrowAsync<OpenRouterNotAuthorizedException>(async () =>
			await client.SendMessageAsync("invalid-api-key", "gpt-4", "Hello"));
	}

	[Fact]
	public async Task ThrowOpenRouterExceptionOnApiError()
	{
		var errorResponse = new ErrorResponse
		{
			Error = new ErrorDetails
			{
				Message = "Model not found",
				Code = 404
			},
			UserId = "test-user"
		};

		SetupHttpResponse(HttpStatusCode.NotFound, errorResponse);

		var client = new OpenRouterClient(httpClientFactoryMock.Object);

		var exception = await Should.ThrowAsync<OpenRouterException>(async () =>
			await client.SendMessageAsync("test-api-key", "invalid-model", "Hello"));

		exception.Message.ShouldContain("Model not found");
	}

	[Fact]
	public async Task ThrowOpenRouterExceptionOnInvalidJsonError()
	{
		SetupHttpResponse(HttpStatusCode.InternalServerError, "Invalid JSON response");

		var client = new OpenRouterClient(httpClientFactoryMock.Object);

		var exception = await Should.ThrowAsync<OpenRouterException>(async () =>
			await client.SendMessageAsync("test-api-key", "gpt-4", "Hello"));

		exception.Message.ShouldContain("Ошибка Open Router");
		exception.Message.ShouldContain("Invalid JSON response");
	}

	[Fact]
	public void ConvertBytesToBase64DataUrl()
	{
		var client = new OpenRouterClient(httpClientFactoryMock.Object);
		var testBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };

		var result = client.ToLocalImageExample(testBytes);

		result.ShouldStartWith("data:image/jpeg;base64,");
		result.ShouldContain(Convert.ToBase64String(testBytes));
	}

	[Fact]
	public void ConvertEmptyBytesToBase64DataUrl()
	{
		var client = new OpenRouterClient(httpClientFactoryMock.Object);
		var emptyBytes = Array.Empty<byte>();

		var result = client.ToLocalImageExample(emptyBytes);

		result.ShouldBe("data:image/jpeg;base64,");
	}

	[Fact]
	public async Task IncludeAuthorizationHeader()
	{
		const string apiKey = "test-api-key-123";
		var responseContent = new ChatCompletionResponse
		{
			Id = "test",
			Model = "test",
			Choices = [new Choice { Message = new ChatMessage { Role = "assistant", Content = "OK" }, FinishReason = "stop" }],
			Usage = new UsageStats()
		};

		HttpRequestMessage? capturedRequest = null;
		httpMessageHandlerMock
			.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>()
			)
			.Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
			.ReturnsAsync(new HttpResponseMessage
			{
				StatusCode = HttpStatusCode.OK,
				Content = new StringContent(JsonSerializer.Serialize(responseContent))
			});

		var client = new OpenRouterClient(httpClientFactoryMock.Object);

		await client.SendMessageAsync(apiKey, "gpt-4", "Test");

		capturedRequest.ShouldNotBeNull();
		capturedRequest.Headers.Authorization.ShouldNotBeNull();
		capturedRequest.Headers.Authorization.Scheme.ShouldBe("Bearer");
		capturedRequest.Headers.Authorization.Parameter.ShouldBe(apiKey);
	}

	private void SetupHttpResponse(HttpStatusCode statusCode, object content)
	{
		var jsonContent = content is string str ? str : JsonSerializer.Serialize(content);

		httpMessageHandlerMock
			.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>()
			)
			.ReturnsAsync(new HttpResponseMessage
			{
				StatusCode = statusCode,
				Content = new StringContent(jsonContent)
			});
	}
}
