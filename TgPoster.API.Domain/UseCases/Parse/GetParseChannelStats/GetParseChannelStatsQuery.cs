using MediatR;

namespace TgPoster.API.Domain.UseCases.Parse.GetParseChannelStats;

/// <summary>
/// Запрос статистики парсинга по источникам текущего пользователя
/// </summary>
/// <param name="Days">Глубина таймлайнов в днях (включая сегодня)</param>
public sealed record GetParseChannelStatsQuery(int Days) : IRequest<ParseChannelStatsResponse>;
