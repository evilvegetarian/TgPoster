using MediatR;

namespace TgPoster.API.Domain.UseCases.Discover.GetClassificationStats;

/// <summary>
///     Запрос статистики классификации обнаруженных каналов
/// </summary>
/// <param name="Days">Глубина таймлайна в днях (включая сегодня)</param>
public sealed record GetClassificationStatsQuery(int Days) : IRequest<ClassificationStatsResponse>;
