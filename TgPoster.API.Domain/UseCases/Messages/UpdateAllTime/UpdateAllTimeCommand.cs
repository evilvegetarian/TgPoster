using MediatR;

namespace TgPoster.API.Domain.UseCases.Messages.UpdateAllTime;

public record UpdateAllTimeCommand(Guid ScheduleId) : IRequest;