using MediatR;

namespace TgPoster.API.Domain.UseCases.Dashboard.GetDashboard;

/// <summary>
/// Запрос данных для главной страницы текущего пользователя
/// </summary>
public sealed record GetDashboardQuery : IRequest<DashboardResponse>;
