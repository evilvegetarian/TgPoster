namespace TgPoster.Telegram.Configuration;

/// <summary>
///     Настройки HTTP-lookup'а публичных страниц t.me (таймауты, ретраи, кэш прокси).
/// </summary>
internal sealed class TelegramPublicLookupOptions
{
	/// <summary>
	///     Таймаут одного HTTP-запроса к t.me в секундах.
	/// </summary>
	public int TimeoutSeconds { get; set; } = 20;

	/// <summary>
	///     Таймаут установки TCP-соединения в секундах.
	/// </summary>
	public int ConnectTimeoutSeconds { get; set; } = 10;

	/// <summary>
	///     Число повторов на транзиентной ошибке (таймаут, 5xx, 429).
	/// </summary>
	public int MaxRetries { get; set; } = 2;

	/// <summary>
	///     База экспоненциального backoff между повторами в миллисекундах.
	/// </summary>
	public int RetryBaseDelayMs { get; set; } = 500;

	/// <summary>
	///     TTL кэша активного HTTP-прокси в секундах.
	/// </summary>
	public int ProxyRefreshSeconds { get; set; } = 60;
}