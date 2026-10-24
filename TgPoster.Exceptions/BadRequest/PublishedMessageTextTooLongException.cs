using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.BadRequest;

/// <summary>
/// Новый текст опубликованного поста вместе с подписью расписания не помещается в лимит Telegram
/// </summary>
/// <param name="limit"></param>
public sealed class PublishedMessageTextTooLongException(int limit)
	: DomainException($"Текст поста вместе с подписью расписания длиннее {limit} символов — лимит Telegram для этого сообщения");
