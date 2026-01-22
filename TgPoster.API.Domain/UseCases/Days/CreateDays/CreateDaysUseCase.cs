using MediatR;
using Security.Interfaces;
using TgPoster.API.Domain.Exceptions;

namespace TgPoster.API.Domain.UseCases.Days.CreateDays;

internal class CreateDaysUseCase(ICreateDaysStorage storage, IIdentityProvider identity)
	: IRequestHandler<CreateDaysCommand>
{
	public async Task Handle(CreateDaysCommand command, CancellationToken ct)
	{
		if (!await storage.ScheduleExistAsync(command.ScheduleId, identity.Current.UserId, ct))
		{
			throw new ScheduleNotFoundException(command.ScheduleId);
		}

		var days = await storage.GetDayOfWeekAsync(command.ScheduleId, ct);
		var duplicateDay = command.DayOfWeekForms.FirstOrDefault(x => days.Contains(x.DayOfWeekPosting));
		if (duplicateDay != null)
		{
			throw new DuplicateDayOfWeekException(duplicateDay.DayOfWeekPosting);
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

		await storage.CreateDaysAsync(daysList, ct);
	}
}