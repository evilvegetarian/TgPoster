namespace TgPoster.API.Domain.Exceptions;

public sealed class TelegramChannelAccessException(string channel, string reason)
	: DomainException($"Не удалось получить доступ к каналу {channel}: {reason}");
