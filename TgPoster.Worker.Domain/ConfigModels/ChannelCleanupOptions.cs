namespace TgPoster.Worker.Domain.ConfigModels;

/// <summary>
/// Настройки фонового выхода из неиспользуемых каналов
/// </summary>
public sealed class ChannelCleanupOptions
{
	/// <summary>
	/// Минимальная пауза между выходами из соседних каналов (секунды)
	/// </summary>
	public int MinDelaySeconds { get; init; } = 5;

	/// <summary>
	/// Максимальная пауза между выходами из соседних каналов (секунды)
	/// </summary>
	public int MaxDelaySeconds { get; init; } = 15;

	/// <summary>
	/// На сколько ограничивать сессию, если Telegram не сообщил длительность (секунды).
	/// Актуально для PEER_FLOOD, где точного времени в ответе нет
	/// </summary>
	public int DefaultCooldownSeconds { get; init; } = 3600;

	/// <summary>
	/// Сколько каналов обрабатывать за один проход. Остаток уезжает новым сообщением,
	/// чтобы один Consume не висел часами и его не перезапустил resume-воркер
	/// </summary>
	public int MaxItemsPerRun { get; init; } = 30;
}
