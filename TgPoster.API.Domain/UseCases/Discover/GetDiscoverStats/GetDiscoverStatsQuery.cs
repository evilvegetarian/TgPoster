using MediatR;

namespace TgPoster.API.Domain.UseCases.Discover.GetDiscoverStats;

/// <summary>
///     Запрос статистики по обнаруженным каналам
/// </summary>
/// <param name="Days">Глубина таймлайнов в днях (включая сегодня)</param>
public sealed record GetDiscoverStatsQuery(int Days) : IRequest<DiscoverStatsResponse>;
