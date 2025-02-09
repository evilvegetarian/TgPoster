using MediatR;
using Security.Interfaces;

namespace TgPoster.Domain.UseCases.Schedules.CreateSchedule;

internal sealed class CreateScheduleUseCase(ICreateScheduleStorage storage, IIdentityProvider identity)
    : IRequestHandler<CreateScheduleCommand, CreateScheduleResponse>
{
    public async Task<CreateScheduleResponse> Handle(CreateScheduleCommand request, CancellationToken cancellationToken)
    {
        var newSchedule = await storage.CreateSchedule(
            request.Name,
            identity.Current.UserId,
            request.TelegramBotId,
            cancellationToken);
        return new CreateScheduleResponse
        {
            Id = newSchedule
        };
    }
}