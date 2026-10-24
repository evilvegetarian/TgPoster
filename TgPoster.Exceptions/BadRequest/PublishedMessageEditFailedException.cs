using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.BadRequest;

/// <summary>
/// Telegram не принял новый текст опубликованного поста
/// </summary>
/// <param name="error"></param>
public sealed class PublishedMessageEditFailedException(string error)
	: DomainException($"Не удалось изменить пост в канале: {error}");
