using MediatR;

namespace TgPoster.API.Domain.UseCases.Schedules.UpdateSchedule;

public sealed record UpdateScheduleCommand(Guid Id, string? Name, Guid? YouTubeAccountId) : IRequest;