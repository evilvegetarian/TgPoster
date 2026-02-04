namespace TgPoster.API.Domain.Exceptions;

public sealed class ChannelNoCommentsException(string channel)
	: DomainException($"Канал {channel} не поддерживает комментарии (нет discussion group)");
