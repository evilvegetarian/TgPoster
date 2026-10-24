using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.BadRequest;

/// <summary>
/// Пост опубликован до сохранения id сообщений, поэтому неизвестно, что удалять из канала
/// </summary>
public sealed class PublishedMessageIdsUnknownException()
	: DomainException("Пост опубликован до обновления — удалите его в Telegram вручную");
