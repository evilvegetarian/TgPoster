using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.NotFound;

/// <summary>
///     Кросс-пост не найден
/// </summary>
public sealed class CrossPostNotFoundException(Guid id)
	: NotFoundException($"Кросс-пост {id} не найден");
