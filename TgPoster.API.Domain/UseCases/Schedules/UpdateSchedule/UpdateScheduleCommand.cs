using MediatR;

namespace TgPoster.API.Domain.UseCases.Schedules.UpdateSchedule;

public sealed record UpdateScheduleCommand(Guid Id, Guid? YouTubeAccountId) : IRequest;