using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.BadRequest;

/// <summary>
/// Bot API не даёт удалять сообщения старше 48 часов
/// </summary>
public sealed class PublishedMessageDeleteExpiredException()
	: DomainException("Telegram даёт боту удалять посты только в течение 48 часов");
