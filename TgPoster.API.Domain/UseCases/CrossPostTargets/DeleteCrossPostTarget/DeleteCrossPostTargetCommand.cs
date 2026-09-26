using MediatR;

namespace TgPoster.API.Domain.UseCases.CrossPostTargets.DeleteCrossPostTarget;

/// <summary>
///     Команда удаления связки расписания с аккаунтом соцсети
/// </summary>
public sealed record DeleteCrossPostTargetCommand(Guid ScheduleId, Guid Id) : IRequest;
