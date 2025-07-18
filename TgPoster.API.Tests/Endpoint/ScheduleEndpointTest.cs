using System.Net;
using Shouldly;
using TgPoster.API.Common;
using TgPoster.API.Domain.UseCases.Schedules.CreateSchedule;
using TgPoster.API.Domain.UseCases.Schedules.ListSchedule;
using TgPoster.API.Models;
using TgPoster.Endpoint.Tests.Helper;

namespace TgPoster.Endpoint.Tests.Endpoint;

public class ScheduleEndpointTest(EndpointTestFixture fixture) : IClassFixture<EndpointTestFixture>
{
    private const string Url = Routes.Schedule.Root;
    private readonly HttpClient client = fixture.AuthClient;
    private readonly CreateHelper create = new(fixture.AuthClient);

    [Fact]
    public async Task Create_WithValidData_ShouldReturnCreated()
    {
        var request = new CreateScheduleRequest
        {
            Name = "Test Schedule",
            TelegramBotId = GlobalConst.Worked.TelegramBotId,
            Channel = GlobalConst.Worked.Channel
        };

        var createdSchedule = await client.PostAsync<CreateScheduleResponse>(Url, request);

        createdSchedule.ShouldNotBeNull();
        createdSchedule.Id.ShouldNotBe(Guid.Empty);
        var schedule = await client.GetAsync<ScheduleResponse>(Url + "/" + createdSchedule.Id);
        schedule.Name.ShouldBe(request.Name);
    }

    [Fact]
    public async Task Create_WithInvalidTelegramBotId_ShouldReturnNotFound()
    {
        var request = new CreateScheduleRequest
        {
            Name = "Test Schedule",
            TelegramBotId = Guid.NewGuid(),
            Channel = GlobalConst.Worked.Channel
        };

        var createdSchedule = await client.PostAsync(Url, request.ToStringContent());
        createdSchedule.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_WithNonExistChannel_ShouldReturnBadRequest()
    {
        var request = new CreateScheduleRequest
        {
            Name = "Test Schedule",
            TelegramBotId = GlobalConst.Worked.TelegramBotId,
            Channel = Guid.NewGuid().ToString()
        };

        var createdSchedule = await client.PostAsync(Url, request.ToStringContent());
        createdSchedule.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task List_ShouldReturnList()
    {
        var schedules = await client.GetAsync<List<ScheduleResponse>>(Url);
        schedules.ShouldNotBeNull();
        schedules.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task Get_WithNonExistingId_ShouldReturnNotFound()
    {
        var nonExistentId = Guid.NewGuid();
        var response = await client.GetAsync(Url + "/" + nonExistentId);
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_WithValidId_ShouldReturnOK()
    {
        var request = new CreateScheduleRequest
        {
            Name = "Schedule to Delete",
            TelegramBotId = GlobalConst.Worked.TelegramBotId,
            Channel = GlobalConst.Worked.Channel
        };
        var createSchedule = await client.PostAsync<CreateScheduleResponse>(Url, request);

        var deleteResponse = await client.DeleteAsync(Url + "/" + createSchedule.Id);
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var response = await client.GetAsync(Url + "/" + createSchedule.Id);
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_WithNonExistingId_ShouldReturnNotFound()
    {
        var nonExistentId = Guid.NewGuid();
        var response = await client.DeleteAsync($"{Url}/{nonExistentId}");
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
    
    [Fact]
    public async Task List_WithAnotherUser_ShouldReturnEmptyList()
    {
        var response = await client.GetAsync<List<ScheduleResponse>>(Url);
        response.ShouldNotBeNull();
        response.Count.ShouldBeGreaterThan(0);

        var anotherClient = fixture.GetClient(fixture.GenerateTestToken(GlobalConst.UserIdEmpty));

        var listAnotherResponse = await anotherClient.GetAsync<List<ScheduleResponse>>(Url);
        listAnotherResponse!.Count.ShouldBeEquivalentTo(0);
    }
}