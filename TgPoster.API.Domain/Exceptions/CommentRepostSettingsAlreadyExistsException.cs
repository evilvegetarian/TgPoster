namespace TgPoster.API.Domain.Exceptions;

public sealed class CommentRepostSettingsAlreadyExistsException(string watchedChannel)
	: DomainException($"Настройки комментирующего репоста для канала {watchedChannel} уже существуют");
