using Shared.OpenRouter.Models.Request;
using Shared.OpenRouter.Models.Response;

namespace Shared.OpenRouter;

/// <summary>
///     Интерфейс клиента для работы с OpenRouter API.
/// </summary>
public interface IOpenRouterClient
{
	/// <summary>
	///     Отправляет текстовое сообщение в чат.
	/// </summary>
	/// <param name="apiKey">API ключ для авторизации.</param>
	/// <param name="model">Название модели для использования.</param>
	/// <param name="message">Текст сообщения.</param>
	/// <param name="cancellationToken">Токен отмены операции.</param>
	/// <returns>Ответ от API.</returns>
	Task<ChatCompletionResponse> SendMessageAsync(
		string apiKey,
		string model,
		string message,
		CancellationToken cancellationToken
	);

	/// <summary>
	///     Отправляет сообщение с изображением.
	/// </summary>
	/// <param name="apiKey">API ключ для авторизации.</param>
	/// <param name="model">Название модели для использования.</param>
	/// <param name="textPrompt">Текстовый промпт.</param>
	/// <param name="imageUrl">URL изображения (может быть base64 data URL).</param>
	/// <param name="cancellationToken">Токен отмены операции.</param>
	/// <returns>Ответ от API.</returns>
	Task<ChatCompletionResponse> SendImageMessageAsync(
		string apiKey,
		string model,
		string textPrompt,
		string imageUrl,
		CancellationToken cancellationToken
	);

	/// <summary>
	///     Отправляет произвольный список сообщений в чат.
	/// </summary>
	/// <param name="apiKey">API ключ для авторизации.</param>
	/// <param name="model">Название модели для использования.</param>
	/// <param name="messages">Список сообщений.</param>
	/// <param name="cancellationToken">Токен отмены операции.</param>
	/// <returns>Ответ от API.</returns>
	Task<ChatCompletionResponse> SendMessageRawAsync(
		string apiKey,
		string model,
		List<ChatMessage> messages,
		CancellationToken cancellationToken
	);

	/// <summary>
	///     Преобразует байты изображения в base64 data URL для отправки.
	/// </summary>
	/// <param name="picture">Байты изображения.</param>
	/// <returns>Data URL изображения.</returns>
	string ToLocalImageDataUrl(byte[] picture);
}