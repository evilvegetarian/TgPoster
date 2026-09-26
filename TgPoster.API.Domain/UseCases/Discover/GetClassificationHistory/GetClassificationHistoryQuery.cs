using MediatR;
using TgPoster.API.Domain.UseCases.Discover.GetClassificationStats;
using TgPoster.API.Domain.UseCases.Messages.ListMessage;

namespace TgPoster.API.Domain.UseCases.Discover.GetClassificationHistory;

/// <summary>
///     Запрос истории классификации: какие каналы и как классифицированы, от самых свежих к старым
/// </summary>
/// <param name="Page">Номер страницы</param>
/// <param name="PageSize">Размер страницы</param>
/// <param name="Search">Подстрока названия или username</param>
/// <param name="Category">Точное название тематики</param>
/// <param name="Confidence">Корзина уверенности модели</param>
/// <param name="From">Начало периода по времени классификации (включительно)</param>
/// <param name="To">Конец периода по времени классификации (включительно)</param>
public sealed record GetClassificationHistoryQuery(
	int Page,
	int PageSize,
	string? Search,
	string? Category,
	ClassificationConfidenceBucket? Confidence,
	DateTimeOffset? From,
	DateTimeOffset? To) : IRequest<PagedResponse<ClassificationHistoryItemResponse>>;
