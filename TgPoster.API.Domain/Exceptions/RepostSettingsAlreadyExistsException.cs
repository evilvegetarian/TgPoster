namespace TgPoster.API.Domain.Exceptions;

public sealed class RepostSettingsAlreadyExistsException(Guid scheduleId)
	: DomainException($"Настройки репоста для расписания {scheduleId} уже существуют");
