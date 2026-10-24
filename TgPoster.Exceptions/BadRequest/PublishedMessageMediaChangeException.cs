using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.BadRequest;

/// <summary>
/// У опубликованного поста нельзя менять медиа и расписание
/// </summary>
public sealed class PublishedMessageMediaChangeException()
	: DomainException("У опубликованного поста можно менять только текст");
