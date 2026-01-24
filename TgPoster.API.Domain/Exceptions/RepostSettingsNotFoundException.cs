namespace TgPoster.API.Domain.Exceptions;

public sealed class RepostSettingsNotFoundException(Guid id)
	: NotFoundException($"Настройки репоста с ID {id} не найдены");
