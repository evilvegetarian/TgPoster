using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.NotFound;

public class PromptSettingNotFoundException(Guid id) : NotFoundException($"Промтов с таким id не существует {id}");