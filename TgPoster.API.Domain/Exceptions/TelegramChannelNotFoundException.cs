namespace TgPoster.API.Domain.Exceptions;

public sealed class TelegramChannelNotFoundException(string channel)
	: DomainException($"Канал {channel} не найден");
