using System.Net;
using System.Net.Http.Json;
using Shouldly;
using TgPoster.API.Common;
using TgPoster.API.Domain.UseCases.Days.GetDayOfWeek;
using TgPoster.API.Domain.UseCases.Days.GetDays;
using TgPoster.API.Models;
using TgPoster.Endpoint.Tests.Helper;

namespace TgPoster.Endpoint.Tests.Endpoint;

public class DayEndpointTest(EndpointTestFixture fixture) : IClassFixture<EndpointTestFixture>
{
    private const string Url = Routes.Day.Root;
    private readonly HttpClient client = fixture.CreateClient();
    private readonly CreateHelper create = new(fixture.CreateClient());

    [Fact]
    public async Task GetDayOfWeek_NonExistScheduleId_ShouldReturnOk()
    {
        var response = await client.GetAsync(Routes.Day.DayOfWeek);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<List<DayOfWeekResponse>>();
        result.ShouldNotBeEmpty();
        result!.Count.ShouldBe(7);
    }

    [Fact]
    public async Task CreateDay_NonExistScheduleId_ShouldReturnOk()
    {
        var request = new CreateDaysRequest
        {
            ScheduleId = Guid.NewGuid(),
            DaysOfWeek = []
        };
        var response = await client.PostAsync(Url, request.ToStringContent());
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateDay_DoubleDayOfWeek_ShouldReturnBadRequest()
    {
        var scheduleId = await create.CreateSchedule();
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
        var response = await client.PostAsync(Url, request.ToStringContent());
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var response2 = await client.PostAsync(Url, request.ToStringContent());
        response2.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateDay_WithValidData_ShouldReturnOk()
    {
        var scheduleId = await create.CreateSchedule();
        var request = new CreateDaysRequest
        {
            ScheduleId = scheduleId,
            DaysOfWeek =
            [
                new DayOfWeekRequest
                {
                    DayOfWeekPosting = DayOfWeek.Monday,
                    StartPosting = new TimeOnly(12, 10),
                    EndPosting = new TimeOnly(18, 00),
                    Interval = 10
                },
                new DayOfWeekRequest
                {
                    DayOfWeekPosting = DayOfWeek.Thursday,
                    StartPosting = new TimeOnly(14, 25),
                    EndPosting = new TimeOnly(18, 00),
                    Interval = 14
                }
            ]
        };
        var response = await client.PostAsync(Url, request.ToStringContent());
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var getResponse = await client.GetAsync<List<GetDaysResponse>>(Url + "?scheduleId=" + scheduleId);
        getResponse.Count.ShouldBe(2);
    }

    [Fact]
    public async Task CreateDay_WithDublicatDayOfWeek_ShouldReturnBadRequest()
    {
        var scheduleId = await create.CreateSchedule();
        var request = new CreateDaysRequest
        {
            ScheduleId = scheduleId,
            DaysOfWeek =
            [
                new DayOfWeekRequest
                {
                    DayOfWeekPosting = DayOfWeek.Monday,
                    StartPosting = new TimeOnly(12, 10),
                    EndPosting = new TimeOnly(18, 00),
                    Interval = 10
                },
                new DayOfWeekRequest
                {
                    DayOfWeekPosting = DayOfWeek.Monday,
                    StartPosting = new TimeOnly(14, 25),
                    EndPosting = new TimeOnly(18, 00),
                    Interval = 14
                }
            ]
        };
        var response = await client.PostAsync(Url, request.ToStringContent());
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateDay_WithEndPostingMoreStartPosting_ShouldReturnBadRequest()
    {
        var scheduleId = await create.CreateSchedule();
        var request = new CreateDaysRequest
        {
            ScheduleId = scheduleId,
            DaysOfWeek =
            [
                new DayOfWeekRequest
                {
                    DayOfWeekPosting = DayOfWeek.Monday,
                    StartPosting = new TimeOnly(18, 55),
                    EndPosting = new TimeOnly(18, 00),
                    Interval = 10
                }
            ]
        };
        var response = await client.PostAsync(Url, request.ToStringContent());
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateDay_WithIntervalMoreTiming_ShouldReturnBadRequest()
    {
        var scheduleId = await create.CreateSchedule();
        var request = new CreateDaysRequest
        {
            ScheduleId = scheduleId,
            DaysOfWeek =
            [
                new DayOfWeekRequest
                {
                    DayOfWeekPosting = DayOfWeek.Monday,
                    StartPosting = new TimeOnly(17, 10),
                    EndPosting = new TimeOnly(18, 00),
                    Interval = 90
                }
            ]
        };
        var response = await client.PostAsync(Url, request.ToStringContent());
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateDay_WithNonExistScheduleId_ShouldReturnNotFound()
    {
        var request = new CreateDaysRequest
        {
            ScheduleId = Guid.NewGuid(),
            DaysOfWeek =
            [
                new DayOfWeekRequest
                {
                    DayOfWeekPosting = DayOfWeek.Monday,
                    StartPosting = new TimeOnly(17, 10),
                    EndPosting = new TimeOnly(18, 00),
                    Interval = 5
                }
            ]
        };
        var response = await client.PostAsync(Url, request.ToStringContent());
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateTimeDay_WithNonExistDayId_ShouldReturnNotFound()
    {
        var nonExistDayId = Guid.NewGuid();
        var upd = new UpdateTimeRequest
        {
            Id = nonExistDayId,
            Times =
            [
                new TimeOnly(10, 15),
                new TimeOnly(20, 30),
                new TimeOnly(20, 40)
            ]
        };
        var response = await client.PatchAsync(Url + "/time", upd.ToStringContent());
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateTimeDay_WithDuplicateTime_ShouldReturnBadRequest()
    {
        var upd = new UpdateTimeRequest
        {
            Id = Guid.NewGuid(),
            Times =
            [
                new TimeOnly(10, 15),
                new TimeOnly(10, 15),
                new TimeOnly(20, 15),
                new TimeOnly(20, 15),
                new TimeOnly(20, 40),
                new TimeOnly(21, 46),
                new TimeOnly(22, 40)
            ]
        };
        var response = await client.PatchAsync(Url + "/time", upd.ToStringContent());
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}