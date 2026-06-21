using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.BadRequest;

public sealed class TelegramChannelAccessException(string channel, string reason)
	: DomainException($"Не удалось получить доступ к каналу {channel}: {reason}");