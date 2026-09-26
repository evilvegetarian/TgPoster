using TgPoster.API.Domain.UseCases.Discover.GetClassificationStats;

namespace TgPoster.API.Models;

/// <summary>
///     Запрос истории классификации обнаруженных каналов
/// </summary>
public sealed class ListClassificationHistoryRequest : PaginationRequest
{
	/// <summary>
	///     Поиск по названию или username
	/// </summary>
	public string? Search { get; init; }

	/// <summary>
	///     Точное название тематики
	/// </summary>
	public string? Category { get; init; }

	/// <summary>
	///     Корзина уверенности модели
	/// </summary>
	public ClassificationConfidenceBucket? Confidence { get; init; }

	/// <summary>
	///     Начало периода по времени классификации (включительно)
	/// </summary>
	public DateTimeOffset? From { get; init; }

	/// <summary>
	///     Конец периода по времени классификации (включительно)
	/// </summary>
	public DateTimeOffset? To { get; init; }
}
