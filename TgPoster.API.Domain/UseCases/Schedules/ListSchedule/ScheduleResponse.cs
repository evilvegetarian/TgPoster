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
}