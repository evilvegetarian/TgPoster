using System.Net;
using Shouldly;
using TgPoster.API.Common;
using TgPoster.API.Models;
using TgPoster.Domain.UseCases.Schedules.CreateSchedule;
using TgPoster.Domain.UseCases.TelegramBots;
using TgPoster.Domain.UseCases.TelegramBots.CreateTelegramBot;

namespace TgPoster.Endpoint.Tests.Helper;

public class CreateHelper(HttpClient client)
{
    private string testTgApi = "8168097685:AAEWcQ8t9H0ser5L-5L1l5Lhu2ym2PFp_Sg";

    public async Task<Guid> CreateSchedule()
    {
        var bot = await CreateTelegramBot();
        var request = new CreateScheduleRequest
        {
            Name = "Test Schedule",
            TelegramBotId = bot
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

    public async Task<Guid> CreateTelegramBot()
    {
        var request = new CreateTelegramBotRequest
        {
            Token = testTgApi
        };
        var response = await client.PostAsync<CreateTelegramBotResponse>(Routes.TelegramBot.Root, request);
        return response.Id;
    }
}