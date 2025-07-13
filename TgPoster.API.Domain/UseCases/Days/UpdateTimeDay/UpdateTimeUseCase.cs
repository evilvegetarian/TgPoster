using MediatR;
using TgPoster.API.Domain.Exceptions;

namespace TgPoster.API.Domain.UseCases.Days.UpdateTimeDay;

internal class UpdateTimeUseCase(IUpdateTimeStorage storage) : IRequestHandler<UpdateTimeCommand>
{
    public async Task Handle(UpdateTimeCommand request, CancellationToken ct)
    {
        if (!await storage.ExistScheduleAsync(request.ScheduleId, ct))
        {
            throw new ScheduleNotFoundException();
        }

        var dayId = await storage.DayIdAsync(request.ScheduleId, request.DayOfWeek, ct);
        if (dayId == Guid.Empty)
        {
            await storage.CreateDayAsync(request.DayOfWeek, request.ScheduleId, request.Times, ct);
        }
        else
        {
            await storage.UpdateTimeDayAsync(dayId, request.Times, ct);
        }
    }
}