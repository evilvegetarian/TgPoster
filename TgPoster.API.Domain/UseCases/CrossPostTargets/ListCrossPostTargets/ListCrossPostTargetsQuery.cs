using MediatR;

namespace TgPoster.API.Domain.UseCases.CrossPostTargets.ListCrossPostTargets;

/// <summary>
///     Запрос списка связок расписания с аккаунтами соцсетей
/// </summary>
public sealed record ListCrossPostTargetsQuery(Guid ScheduleId) : IRequest<List<CrossPostTargetResponse>>;
