namespace TgPoster.API.Configuration;

/// <summary>
/// Конфигурация для трассировки (distributed tracing)
/// </summary>
public class TracingConfiguration
{
	/// <summary>
	/// URL эндпоинта для отправки данных трассировки
	/// </summary>
	public required string Url { get; set; }
}