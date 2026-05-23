using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.BadRequest;

public sealed class RepostDestinationAlreadyExistsException(string chatIdentifier)
	: DomainException($"Канал {chatIdentifier} уже добавлен в список целевых каналов");
