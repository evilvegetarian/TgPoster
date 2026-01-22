namespace TgPoster.API.Domain.UseCases.Schedules.ListSchedule;

public sealed class ScheduleResponse
{
	/// <summary>
	///     Id расписания
	/// </summary>
	public required Guid Id { get; init; }

	/// <summary>
	///     Название расписание
	/// </summary>
	public required string Name { get; init; }

	/// <summary>
	///     Активность расписания
	/// </summary>
	public required bool IsActive { get; init; }

	/// <summary>
	///     Канал для которого расписание
	/// </summary>
	public required string ChannelName { get; init; }

	/// <summary>
	///     Название телеграмм бота
	/// </summary>
	public required string BotName { get; init; }

	/// <summary>
	///     Id настройки подключения к open router
	/// </summary>
	public Guid? OpenRouterId { get; init; }

	/// <summary>
	///     Id промтов
	/// </summary>
	public Guid? PromptId { get; init; }

	/// <summary>
	///     Id YouTube аккаунта
	/// </summary>
	public Guid? YouTubeAccountId { get; init; }
}