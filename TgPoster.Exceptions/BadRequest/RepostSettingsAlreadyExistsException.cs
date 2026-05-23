using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.BadRequest;

public sealed class RepostSettingsAlreadyExistsException(Guid scheduleId)
	: DomainException($"Настройки репоста для расписания {scheduleId} уже существуют");
