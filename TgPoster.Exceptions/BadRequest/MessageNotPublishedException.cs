using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.BadRequest;

/// <summary>
/// Удалить из канала можно только опубликованный пост
/// </summary>
/// <param name="id"></param>
public sealed class MessageNotPublishedException(Guid id)
	: DomainException($"Сообщение с ID {id} не опубликовано, удалить из канала можно только опубликованный пост");
