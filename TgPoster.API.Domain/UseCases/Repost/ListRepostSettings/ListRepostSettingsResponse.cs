namespace TgPoster.API.Domain.UseCases.Repost.ListRepostSettings;

/// <summary>
///     Элемент списка настроек репоста.
/// </summary>
public sealed record RepostSettingsItemDto
{
	/// <summary>
	///     Id настроек репоста.
	/// </summary>
	public required Guid Id { get; init; }

	/// <summary>
	///     Id расписания.
	/// </summary>
	public required Guid ScheduleId { get; init; }

	/// <summary>
	///     Название расписания.
	/// </summary>
	public required string ScheduleName { get; init; }

	/// <summary>
	///     Id телеграм сессии.
	/// </summary>
	public required Guid TelegramSessionId { get; init; }

	/// <summary>
	///     Активность настроек репоста.
	/// </summary>
	public required bool IsActive { get; init; }

	/// <summary>
	///     Количество каналов для репоста.
	/// </summary>
	public required int DestinationsCount { get; init; }

	/// <summary>
	/// Количество включённых каналов: в них воркер делает репост
	/// </summary>
	public required int ActiveDestinationsCount { get; init; }

	/// <summary>
	/// Количество каналов, где аккаунт забанен или вышел из чата
	/// </summary>
	public required int ProblemDestinationsCount { get; init; }
}

/// <summary>
///     Ответ со списком настроек репоста.
/// </summary>
public sealed record ListRepostSettingsResponse
{
	public required List<RepostSettingsItemDto> Items { get; init; }
}