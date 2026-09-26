using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.NotFound;

/// <summary>
///     Настройка кросс-постинга не найдена
/// </summary>
public sealed class CrossPostTargetNotFoundException(Guid id)
	: NotFoundException($"Настройка кросс-постинга {id} не найдена");
