using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.BadRequest;

public sealed class CommentRepostSettingsAlreadyExistsException(string watchedChannel)
	: DomainException($"Настройки комментирующего репоста для канала {watchedChannel} уже существуют");
