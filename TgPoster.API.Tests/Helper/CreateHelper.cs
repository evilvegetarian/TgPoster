using System.Net;
using Shouldly;
using TgPoster.API.Common;
using TgPoster.API.Models;
using TgPoster.Domain.UseCases.Schedules.CreateSchedule;

namespace TgPoster.Endpoint.Tests.Helper;

public class CreateHelper(HttpClient client)
{
    public async Task<Guid> CreateSchedule()
    {
        var request = new CreateScheduleRequest
        {
            Name = "Test Schedule",
        };
        var response = await client.PostAsync<CreateScheduleResponse>(Routes.Schedule.Root, request);
        return response.Id;
    }

    public async Task CreateDay(Guid scheduleId)
    {
        var request = new CreateDaysRequest
        {
            ScheduleId = scheduleId,
            DaysOfWeek =
            [
                new DayOfWeekRequest
                {
                    DayOfWeekPosting = DayOfWeek.Monday,
                    StartPosting = new TimeOnly(10, 15),
                    EndPosting = new TimeOnly(20, 30),
                    Interval = 45
                }
            ]
        };
        var response = await client.PostAsync(Routes.Day.Root + "?scheduleId=" + scheduleId, request.ToStringContent());
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}