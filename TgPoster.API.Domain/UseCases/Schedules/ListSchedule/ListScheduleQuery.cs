using MediatR;

namespace TgPoster.API.Domain.UseCases.Schedules.ListSchedule;

public sealed class ListScheduleQuery : IRequest<ScheduleListResponse>;