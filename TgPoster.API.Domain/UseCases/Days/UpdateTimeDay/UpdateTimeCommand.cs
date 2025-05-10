using MediatR;
using TgPoster.API.Domain.Exceptions;

namespace TgPoster.API.Domain.UseCases.Days.UpdateTimeDay;

public sealed record UpdateTimeCommand(Guid Id, List<TimeOnly> Times) : IRequest;

internal class UpdateTimeUseCase(IUpdateTimeStorage storage) : IRequestHandler<UpdateTimeCommand>
{
    public async Task Handle(UpdateTimeCommand request, CancellationToken ct)
    {
        if (!await storage.DayExistAsync(request.Id, ct))
        {
            throw new DaysNotFoundException();
        }

        await storage.UpdateTimeDayAsync(request.Id, request.Times, ct);
    }
}