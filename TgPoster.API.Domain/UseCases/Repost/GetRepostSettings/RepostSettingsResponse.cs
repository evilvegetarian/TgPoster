namespace TgPoster.API.Domain.UseCases.Repost.GetRepostSettings;

/// <summary>
///     Подробная информация о настройках репоста.
/// </summary>
public sealed record RepostSettingsResponse
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
	///     Название телеграм сессии.
	/// </summary>
	public required string? TelegramSessionName { get; init; }

	/// <summary>
	///     Активность настроек репоста.
	/// </summary>
	public required bool IsActive { get; init; }

	/// <summary>
	///     Общая минимальная задержка перед репостом (секунды), копируется на новые каналы
	/// </summary>
	public required int DefaultDelayMinSeconds { get; init; }

	/// <summary>
	///     Общая максимальная задержка перед репостом (секунды), копируется на новые каналы
	/// </summary>
	public required int DefaultDelayMaxSeconds { get; init; }

	/// <summary>
	///     Общая настройка "репостить каждое N-е сообщение" (1 = каждое), копируется на новые каналы
	/// </summary>
	public required int DefaultRepostEveryNth { get; init; }

	/// <summary>
	///     Общая вероятность пропуска репоста (0-100%), копируется на новые каналы
	/// </summary>
	public required int DefaultSkipProbability { get; init; }

	/// <summary>
	///     Общий лимит репостов в день (null = без лимита), копируется на новые каналы
	/// </summary>
	public required int? DefaultMaxRepostsPerDay { get; init; }

	/// <summary>
	///     Дата создания.
	/// </summary>
	public required DateTimeOffset Created { get; init; }

	/// <summary>
	///     Список каналов для репоста.
	/// </summary>
	public required List<RepostDestinationDto> Destinations { get; init; }
}