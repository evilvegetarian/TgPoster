using MediatR;

namespace TgPoster.API.Domain.UseCases.Messages.GetTime;

public sealed record GetTimeCommand(Guid ScheduleId) : IRequest<GetTimeResponse>;