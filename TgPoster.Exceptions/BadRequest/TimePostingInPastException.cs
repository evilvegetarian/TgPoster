using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.BadRequest;

/// <summary>
/// Время публикации неопубликованного поста уже прошло
/// </summary>
public sealed class TimePostingInPastException()
	: DomainException("Текущее время больше времени поста");
