using System.Net;
using Shouldly;
using TgPoster.API.Common;
using TgPoster.API.Domain.UseCases.Schedules.CreateSchedule;
using TgPoster.API.Models;

namespace TgPoster.Endpoint.Tests.Helper;

public class CreateHelper(HttpClient client)
{
    private readonly string testTgApi = "8168097685:AAEWcQ8t9H0ser5L-5L1l5Lhu2ym2PFp_Sg";

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