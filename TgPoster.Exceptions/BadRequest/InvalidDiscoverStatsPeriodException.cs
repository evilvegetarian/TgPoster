using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.BadRequest;

/// <summary>
///     Исключение, выбрасываемое когда запрошенная глубина статистики Discover вне допустимого диапазона
/// </summary>
/// <param name="days">Указанная глубина в днях</param>
/// <param name="maxDays">Максимально допустимая глубина в днях</param>
public sealed class InvalidDiscoverStatsPeriodException(int days, int maxDays)
	: DomainException($"Глубина статистики должна быть от 1 до {maxDays} дней. Указано: {days}");
