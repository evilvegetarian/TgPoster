namespace TgPoster.API.Domain.Exceptions;

public sealed class RepostSettingsNotFoundException : NotFoundException
{
	public RepostSettingsNotFoundException(Guid id)
		: base($"Настройки репоста с ID {id} не найдены")
	{
	}

	private RepostSettingsNotFoundException(string message) : base(message)
	{
	}

	public static RepostSettingsNotFoundException ForSchedule(Guid scheduleId) =>
		new($"Настройки репоста для расписания {scheduleId} не найдены");
}
