namespace TgPoster.Worker.Domain.UseCases.ClassifyChannel;

public interface IClassifyChannelStorage
{
	/// <summary>
	///     Прочитать настройки классификатора; null, если их ещё никто не сохранял
	/// </summary>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<ClassifierSettingsDto?> GetSettingsAsync(CancellationToken ct);

	/// <summary>
	///     Создать запись настроек из переданных значений, если её ещё нет; существующую не трогать
	/// </summary>
	/// <param name="defaults"></param>
	/// <param name="ct"></param>
	Task EnsureSettingsAsync(ClassifierSettingsDto defaults, CancellationToken ct);

	/// <summary>
	///     Выбрать каналы для классификации: сначала ни разу не классифицированные, среди них — давно не пробованные.
	///     Неудачные попытки откладываются до <paramref name="retryBefore" />, повторная классификация — только
	///     для классифицированных раньше <paramref name="reclassifyBefore" />
	/// </summary>
	/// <param name="batchSize"></param>
	/// <param name="retryBefore"></param>
	/// <param name="reclassifyBefore"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<List<ChannelForClassificationDto>> GetChannelsToClassifyAsync(
		int batchSize,
		DateTimeOffset retryBefore,
		DateTimeOffset? reclassifyBefore,
		CancellationToken ct
	);

	/// <summary>
	///     Отметить, что классификатор взялся за канал: неудачный канал уйдёт в конец очереди
	/// </summary>
	/// <param name="id"></param>
	/// <param name="attemptedAt"></param>
	/// <param name="ct"></param>
	Task MarkClassificationAttemptAsync(Guid id, DateTimeOffset attemptedAt, CancellationToken ct);

	Task UpdateClassificationAsync(
		Guid id,
		string? category,
		string? subcategory,
		string[]? tags,
		string? language,
		double? confidence,
		CancellationToken ct
	);

	Task MarkChannelBannedAsync(Guid channelId, CancellationToken ct);
}

public sealed class ChannelForClassificationDto
{
	public required Guid Id { get; init; }
	public required string? Title { get; init; }
	public required string? Description { get; init; }
	public required string? Username { get; init; }
	public required long? TelegramId { get; init; }
}

/// <summary>
///     Настройки классификатора, с которыми работает воркер
/// </summary>
public sealed record ClassifierSettingsDto
{
	public required bool IsEnabled { get; init; }
	public required string Model { get; init; }
	public required int BatchSize { get; init; }
	public required int IntervalMinutes { get; init; }
	public required int MessageSampleCount { get; init; }
	public required int PhotoCount { get; init; }
	public int? ReclassifyAfterDays { get; init; }
	public required IReadOnlyList<string> Categories { get; init; }
	public required string SystemPrompt { get; init; }
}
