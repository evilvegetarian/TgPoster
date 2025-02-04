using MediatR;
using TgPoster.Domain.Exceptions;

namespace TgPoster.Domain.UseCases.Days.UpdateTimeDay;

public sealed record UpdateTimeCommand(Guid Id, List<TimeOnly> Times) : IRequest;

internal class UpdateTimeUseCase(IUpdateTimeStorage storage) : IRequestHandler<UpdateTimeCommand>
{
    public async Task Handle(UpdateTimeCommand request, CancellationToken cancellationToken)
    {
        if (!await storage.DayExistAsync(request.Id,cancellationToken))
        {
            throw new DaysNotFoundException();
        }
        await storage.UpdateTimeDayAsync(request.Id, request.Times, cancellationToken);
    }
}