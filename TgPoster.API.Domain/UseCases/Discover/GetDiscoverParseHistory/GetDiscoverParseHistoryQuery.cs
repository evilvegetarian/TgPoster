using MediatR;
using TgPoster.API.Domain.UseCases.Messages.ListMessage;

namespace TgPoster.API.Domain.UseCases.Discover.GetDiscoverParseHistory;

/// <summary>
///     Запрос истории парсинга: какие каналы и когда парсились, от самых свежих к старым
/// </summary>
/// <param name="Page">Номер страницы</param>
/// <param name="PageSize">Размер страницы</param>
/// <param name="Search">Подстрока названия или username</param>
/// <param name="From">Начало периода по времени парсинга (включительно)</param>
/// <param name="To">Конец периода по времени парсинга (включительно)</param>
public sealed record GetDiscoverParseHistoryQuery(
	int Page,
	int PageSize,
	string? Search,
	DateTimeOffset? From,
	DateTimeOffset? To) : IRequest<PagedResponse<DiscoverParseHistoryItemResponse>>;
