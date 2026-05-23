namespace TgPoster.Exceptions;

public class PromptSettingNotFoundException(Guid id) : NotFoundException($"Промтов с таким id не существует {id}");
