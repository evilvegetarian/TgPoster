using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.BadRequest;

public sealed class ChannelNoCommentsException(string channel)
	: DomainException($"Канал {channel} не поддерживает комментарии (нет discussion group)");
