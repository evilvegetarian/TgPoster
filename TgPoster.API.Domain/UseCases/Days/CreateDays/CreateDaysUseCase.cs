using MediatR;
using Security.Interfaces;
using TgPoster.API.Domain.Exceptions;

namespace TgPoster.API.Domain.UseCases.Days.CreateDays;

internal class CreateDaysUseCase(ICreateDaysStorage storage, IIdentityProvider identity)
    : IRequestHandler<CreateDaysCommand>
{
    public async Task Handle(CreateDaysCommand command, CancellationToken cancellationToken)
    {
        if (!await storage.ScheduleExistAsync(command.ScheduleId, identity.Current.UserId, cancellationToken))
        {
            throw new ScheduleNotFoundException();
        }

        var days = await storage.GetDayOfWeek(command.ScheduleId, cancellationToken);
        if (command.DayOfWeekForms.Any(x => days.Contains(x.DayOfWeekPosting)))
        {
            throw new ArgumentException();
        }

        List<CreateDayDto> daysList = [];
        foreach (var dayForm in command.DayOfWeekForms)
        {
            var newDay = new CreateDayDto
            {
                ScheduleId = command.ScheduleId,
                DayOfWeek = dayForm.DayOfWeekPosting
            };
            var i = dayForm.StartPosting;
            while (i <= dayForm.EndPosting)
            {
                newDay.TimePostings.Add(i);
                i = i.AddMinutes(dayForm.Interval);
            }

            daysList.Add(newDay);
        }

        await storage.CreateDaysAsync(daysList, cancellationToken);
    }
}