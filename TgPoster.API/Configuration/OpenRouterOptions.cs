namespace TgPoster.API.Configuration;

/// <summary>
/// Настройки для OpenRouter
/// </summary>
public class OpenRouterOptions
{
	/// <summary>
	/// Секретный ключ для доступа к OpenRouter API
	/// </summary>
	public required string SecretKey { get; set; }
}