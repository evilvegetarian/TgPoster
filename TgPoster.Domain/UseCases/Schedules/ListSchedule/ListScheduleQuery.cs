using MediatR;

namespace TgPoster.Domain.UseCases.Schedules.ListSchedule;

public sealed class ListScheduleQuery : IRequest<List<ScheduleResponse>>;