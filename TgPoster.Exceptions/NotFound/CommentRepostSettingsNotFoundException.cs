namespace TgPoster.Exceptions;

public sealed class CommentRepostSettingsNotFoundException(Guid id)
	: NotFoundException($"Настройки комментирующего репоста с ID {id} не найдены");
