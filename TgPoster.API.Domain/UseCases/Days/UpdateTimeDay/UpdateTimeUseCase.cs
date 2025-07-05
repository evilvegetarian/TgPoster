using MediatR;
using TgPoster.API.Domain.Exceptions;

namespace TgPoster.API.Domain.UseCases.Days.UpdateTimeDay;

internal class UpdateTimeUseCase(IUpdateTimeStorage storage) : IRequestHandler<UpdateTimeCommand>
{
    public async Task Handle(UpdateTimeCommand request, CancellationToken ct)
    {
        var dayId = await storage.DayIdAsync(request.ScheduleId, request.DayOfWeek, ct);
        if (dayId==Guid.Empty)
        {
            throw new DaysNotFoundException();
        }

        await storage.UpdateTimeDayAsync(dayId, request.Times, ct);
    }
}