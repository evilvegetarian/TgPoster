using MediatR;
using Security.Interfaces;
using TgPoster.API.Domain.Exceptions;

namespace TgPoster.API.Domain.UseCases.Days.UpdateTimeDay;

internal class UpdateTimeUseCase(IUpdateTimeStorage storage, IIdentityProvider provider)
    : IRequestHandler<UpdateTimeCommand>
{
    public async Task Handle(UpdateTimeCommand request, CancellationToken ct)
    {
        if (!await storage.ExistScheduleAsync(request.ScheduleId, provider.Current.UserId, ct))
        {
            throw new ScheduleNotFoundException(request.ScheduleId);
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