using System.Net;
using Bogus;
using Shouldly;
using TgPoster.API.Common;
using TgPoster.API.Models;
using TgPoster.Domain.UseCases.Schedules.CreateSchedule;
using TgPoster.Domain.UseCases.Schedules.ListSchedule;
using TgPoster.Endpoint.Tests.Helper;

namespace TgPoster.Endpoint.Tests;

public class ScheduleEndpointTest(EndpointTestFixture fixture) : IClassFixture<EndpointTestFixture>
{
    private readonly HttpClient client = fixture.CreateClient();
    private readonly string Url = Routes.Schedule.Root;

    [Fact]
    public async Task Create_ShouldReturnOk_WithCreatedSchedule()
    {
        var request = new CreateScheduleRequest
        {
            Name = "Test Schedule"
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
        for (int i = 0; i < 5; i++)
        {
            await CreateSchedule();
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
        var request = new CreateScheduleRequest { Name = "Schedule to Delete" };
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

    private async Task<Guid> CreateSchedule()
    {
        var request = new CreateScheduleRequest
        {
            Name = "Test Schedule",
        };
        var response = await client.PostAsync<CreateScheduleResponse>(Url, request);
        return response.Id;
    }
}