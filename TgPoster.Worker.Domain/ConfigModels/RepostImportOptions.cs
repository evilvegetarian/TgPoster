namespace TgPoster.Worker.Domain.ConfigModels;

/// <summary>
///     Настройки фоновой обработки заданий на массовое добавление целевых каналов.
/// </summary>
public sealed class RepostImportOptions
{
	/// <summary>
	///     Минимальная пауза между обращениями к Telegram по соседним каналам (секунды).
	/// </summary>
	public int MinDelaySeconds { get; init; } = 15;

	/// <summary>
	///     Максимальная пауза между обращениями к Telegram по соседним каналам (секунды).
	/// </summary>
	public int MaxDelaySeconds { get; init; } = 45;

	/// <summary>
	///     На сколько ограничивать сессию, если Telegram не сообщил длительность (секунды).
	///     Актуально для PEER_FLOOD, где точного времени в ответе нет.
	/// </summary>
	public int DefaultCooldownSeconds { get; init; } = 3600;
}
