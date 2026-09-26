using System.ComponentModel.DataAnnotations;
using TgPoster.API.Domain.UseCases.Discover.GetDiscoverStats;

namespace TgPoster.API.Domain.UseCases.Discover.GetClassificationStats;

/// <summary>
///     Статистика LLM-классификации обнаруженных каналов: покрытие, свежесть, качество и разбивки
/// </summary>
public sealed record ClassificationStatsResponse
{
	/// <summary>Итоговые счётчики по всей базе</summary>
	[Required]
	public required ClassificationStatsTotals Totals { get; init; }

	/// <summary>Сколько каналов классифицировано за последние периоды</summary>
	[Required]
	public required ClassificationStatsFreshness Freshness { get; init; }

	/// <summary>Количество каналов и средняя уверенность по каждой тематике, по убыванию количества</summary>
	[Required]
	public required List<ClassificationCategoryStat> ByCategory { get; init; }

	/// <summary>Количество каналов по языку, по убыванию</summary>
	[Required]
	public required List<DiscoverNamedCount> ByLanguage { get; init; }

	/// <summary>Распределение классифицированных каналов по уверенности модели (все корзины, включая нулевые)</summary>
	[Required]
	public required List<ClassificationConfidenceBucketCount> ByConfidence { get; init; }

	/// <summary>Самые частые подкатегории, по убыванию</summary>
	[Required]
	public required List<ClassificationSubcategoryStat> TopSubcategories { get; init; }

	/// <summary>Самые частые теги (в нижнем регистре), по убыванию</summary>
	[Required]
	public required List<DiscoverNamedCount> TopTags { get; init; }

	/// <summary>Сколько каналов классифицировано в каждый день окна (все дни, включая нулевые)</summary>
	[Required]
	public required List<DiscoverDailyCount> ClassifiedByDay { get; init; }
}

/// <summary>
///     Итоговые счётчики классификации
/// </summary>
public sealed record ClassificationStatsTotals
{
	/// <summary>Всего каналов (без забаненных)</summary>
	public required int Total { get; init; }

	/// <summary>Доступно для классификации: публичные каналы с username</summary>
	public required int Eligible { get; init; }

	/// <summary>Приватные каналы без username: классификатор их пропускает</summary>
	public required int Skipped { get; init; }

	/// <summary>Классифицировано хотя бы раз</summary>
	public required int Classified { get; init; }

	/// <summary>В очереди: доступны для классификации, но ещё ни разу не классифицированы</summary>
	public required int Pending { get; init; }

	/// <summary>Из очереди: классификатор уже пробовал, но безуспешно — повторит позже</summary>
	public required int Failed { get; init; }

	/// <summary>Каналов с проставленной тематикой</summary>
	public required int WithCategory { get; init; }

	/// <summary>Каналов хотя бы с одним тегом</summary>
	public required int WithTags { get; init; }

	/// <summary>Средняя уверенность модели (0.0–1.0) по классифицированным каналам; null, если таких нет</summary>
	public double? AverageConfidence { get; init; }

	/// <summary>Сколько разных пар «тематика — подкатегория»</summary>
	public required int DistinctSubcategories { get; init; }

	/// <summary>Сколько разных тегов (без учёта регистра)</summary>
	public required int DistinctTags { get; init; }
}

/// <summary>
///     Свежесть классификации: сколько каналов классифицировано за последние периоды
/// </summary>
public sealed record ClassificationStatsFreshness
{
	/// <summary>Классифицировано за последние 24 часа</summary>
	public required int ClassifiedLast24Hours { get; init; }

	/// <summary>Классифицировано за последние 7 дней</summary>
	public required int ClassifiedLast7Days { get; init; }

	/// <summary>Классифицировано за последние 30 дней</summary>
	public required int ClassifiedLast30Days { get; init; }

	/// <summary>Когда последний раз классифицировался какой-либо канал</summary>
	public DateTimeOffset? LastClassifiedAt { get; init; }
}

/// <summary>
///     Тематика: сколько в ней каналов и насколько модель в ней уверена
/// </summary>
public sealed record ClassificationCategoryStat
{
	/// <summary>Тематика</summary>
	public required string Name { get; init; }

	/// <summary>Количество каналов</summary>
	public required int Count { get; init; }

	/// <summary>Средняя уверенность модели (0.0–1.0) по каналам этой тематики</summary>
	public double? AverageConfidence { get; init; }
}

/// <summary>
///     Подкатегория внутри тематики и сколько в ней каналов
/// </summary>
public sealed record ClassificationSubcategoryStat
{
	/// <summary>Тематика, к которой относится подкатегория</summary>
	public string? Category { get; init; }

	/// <summary>Подкатегория</summary>
	public required string Subcategory { get; init; }

	/// <summary>Количество каналов</summary>
	public required int Count { get; init; }
}

/// <summary>
///     Корзина по уверенности модели в классификации
/// </summary>
public enum ClassificationConfidenceBucket
{
	/// <summary>Уверенность не записана</summary>
	Unknown,

	/// <summary>Меньше 0.5 — по промпту модель ставит такую уверенность, когда данных мало</summary>
	UpTo50,

	/// <summary>От 0.5 до 0.7</summary>
	From50To70,

	/// <summary>От 0.7 до 0.8</summary>
	From70To80,

	/// <summary>От 0.8 до 0.9</summary>
	From80To90,

	/// <summary>0.9 и выше</summary>
	Over90
}

/// <summary>
///     Количество каналов в одной корзине по уверенности
/// </summary>
public sealed record ClassificationConfidenceBucketCount
{
	/// <summary>Корзина</summary>
	public required ClassificationConfidenceBucket Bucket { get; init; }

	/// <summary>Количество каналов</summary>
	public required int Count { get; init; }
}
