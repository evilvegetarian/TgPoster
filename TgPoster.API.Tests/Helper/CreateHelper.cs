using System.Net;
using Shouldly;
using TgPoster.API.Common;
using TgPoster.API.Domain.UseCases.Schedules.CreateSchedule;
using TgPoster.API.Models;

namespace TgPoster.Endpoint.Tests.Helper;

public class CreateHelper(HttpClient client)
{
    public async Task<Guid> CreateSchedule()
    {
        var request = new CreateScheduleRequest
        {
            Name = "Test Schedule",
            TelegramBotId = GlobalConst.Worked.TelegramBotId,
            Channel = GlobalConst.Worked.Channel
        };
        var response = await client.PostAsync<CreateScheduleResponse>(Routes.Schedule.Create, request);
        return response.Id;
    }

    public async Task CreateDay(Guid scheduleId, DayOfWeek? dayOfWeek)
    {
        var request = new CreateDaysRequest
        {
            ScheduleId = scheduleId,
            DaysOfWeek =
            [
                new DayOfWeekRequest
                {
                    DayOfWeekPosting = dayOfWeek ?? DayOfWeek.Monday,
                    StartPosting = new TimeOnly(10, 15),
                    EndPosting = new TimeOnly(20, 30),
                    Interval = 45
                }
            ]
        };
        var response = await client.PostAsync(Routes.Day.Root + "?scheduleId=" + scheduleId, request.ToStringContent());
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    public async Task CreateDay()
    {
        var scheduleId = await CreateSchedule();
        await CreateDay(scheduleId, null);
    }
}