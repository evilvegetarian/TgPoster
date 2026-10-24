using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.BadRequest;

/// <summary>
/// Telegram не удалил пост из канала
/// </summary>
/// <param name="error"></param>
public sealed class PublishedMessageDeleteFailedException(string error)
	: DomainException($"Не удалось удалить пост из канала: {error}");
