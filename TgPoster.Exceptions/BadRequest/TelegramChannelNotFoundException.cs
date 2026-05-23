using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.BadRequest;

public sealed class TelegramChannelNotFoundException(string channel)
	: DomainException($"Канал {channel} не найден");
