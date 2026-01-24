namespace TgPoster.API.Domain.Exceptions;

public sealed class RepostDestinationAlreadyExistsException(string chatIdentifier)
	: DomainException($"Канал {chatIdentifier} уже добавлен в список целевых каналов");
