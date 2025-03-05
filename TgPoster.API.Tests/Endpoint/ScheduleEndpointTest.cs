using System.Net;
using Shouldly;
using TgPoster.API.Common;
using TgPoster.API.Models;
using TgPoster.Domain.UseCases.Schedules.CreateSchedule;
using TgPoster.Domain.UseCases.Schedules.ListSchedule;
using TgPoster.Endpoint.Tests.Helper;
using TgPoster.Endpoint.Tests.Seeder;

namespace TgPoster.Endpoint.Tests.Endpoint;

public class ScheduleEndpointTest(EndpointTestFixture fixture) : IClassFixture<EndpointTestFixture>
{
    private const string Url = Routes.Schedule.Root;
    private readonly HttpClient client = fixture.CreateClient();
    private readonly CreateHelper create = new(fixture.CreateClient());

    [Fact]
    public async Task Create_ShouldReturnOk_WithCreatedSchedule()
    {
        var request = new CreateScheduleRequest
        {
            Name = "Test Schedule",
            TelegramBotId = GlobalConst.TelegramNotWorkedBotId
        };

        var createdSchedule = await client.PostAsync<CreateScheduleResponse>(Url, request);

        createdSchedule.ShouldNotBeNull();
        createdSchedule.Id.ShouldNotBe(Guid.Empty);
        var schedule = await client.GetAsync<ScheduleResponse>(Url + "/" + createdSchedule.Id);
        schedule.Name.ShouldBe(request.Name);
    }

    [Fact]
    public async Task List_ShouldReturnOk_WithSchedules()
    {
        for (var i = 0; i < 5; i++)
        {
            await create.CreateSchedule();
        }

        var schedules = await client.GetAsync<List<ScheduleResponse>>(Url);
        schedules.ShouldNotBeNull();
        schedules.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task Get_ShouldReturnNotFound_ForNonExistentSchedule()
    {
        var nonExistentId = Guid.NewGuid();
        var response = await client.GetAsync(Url + "/" + nonExistentId);
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_ShouldReturnOk_WhenScheduleExists()
    {
        var request = new CreateScheduleRequest
        {
            Name = "Schedule to Delete",
            TelegramBotId = GlobalConst.TelegramNotWorkedBotId
        };
        var createSchedule = await client.PostAsync<CreateScheduleResponse>(Url, request);

        var deleteResponse = await client.DeleteAsync(Url + "/" + createSchedule.Id);
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var response = await client.GetAsync(Url + "/" + createSchedule.Id);
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenScheduleDoesNotExist()
    {
        var nonExistentId = Guid.NewGuid();

        var response = await client.DeleteAsync($"{Url}/{nonExistentId}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}