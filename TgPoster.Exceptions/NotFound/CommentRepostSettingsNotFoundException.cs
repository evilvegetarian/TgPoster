using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.NotFound;

public sealed class CommentRepostSettingsNotFoundException(Guid id)
	: NotFoundException($"Настройки комментирующего репоста с ID {id} не найдены");
