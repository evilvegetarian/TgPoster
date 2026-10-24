using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.BadRequest;

/// <summary>
/// Пост опубликован до сохранения id сообщений, поэтому неизвестно, где в канале лежит его текст
/// </summary>
public sealed class PublishedMessageTextNotEditableException()
	: DomainException("Пост опубликован до обновления — текст можно изменить только в Telegram");
