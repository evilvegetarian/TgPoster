using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.BadRequest;

/// <summary>
///     Повтор кросс-поста запрещён
/// </summary>
public sealed class CrossPostRetryNotAllowedException()
	: DomainException("Повторить можно только неудавшийся или пропущенный кросс-пост");
