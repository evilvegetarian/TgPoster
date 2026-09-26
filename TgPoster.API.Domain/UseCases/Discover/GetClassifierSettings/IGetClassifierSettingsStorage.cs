namespace TgPoster.API.Domain.UseCases.Discover.GetClassifierSettings;

/// <summary>
///     Сохранённые настройки классификатора, как они лежат в БД
/// </summary>
public sealed record SavedClassifierSettingsDto
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
	public ClassifierSessionInfo? TelegramSession { get; init; }
	public DateTimeOffset? UpdatedAt { get; init; }
}

public interface IGetClassifierSettingsStorage
{
	/// <summary>
	///     Прочитать сохранённые настройки классификатора; null, если их ещё нет
	/// </summary>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<SavedClassifierSettingsDto?> GetClassifierSettingsAsync(CancellationToken ct);
}
