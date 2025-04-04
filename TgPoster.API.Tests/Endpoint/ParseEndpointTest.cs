using System.Net;
using Shouldly;
using TgPoster.API.Common;
using TgPoster.API.Models;
using TgPoster.Endpoint.Tests.Helper;

namespace TgPoster.Endpoint.Tests.Endpoint;

public class ParseEndpointTest(EndpointTestFixture fixture) : IClassFixture<EndpointTestFixture>
{
    private const string Url = Routes.Parse.Root;
    private readonly HttpClient client = fixture.CreateClient();
    private readonly CreateHelper create = new(fixture.CreateClient());

    [Fact]
    public async Task Create_WithInValidChannel_ShouldBadRequest()
    {
        var request = new ParseChannelRequest
        {
            Channel = "",
            AlwaysCheckNewPosts = false,
            ScheduleId = Guid.NewGuid()
        };

        var createdSchedule = await client.PostAsync(Url, request.ToStringContent());
        createdSchedule.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WithInValidDateFrom_ShouldBadRequest()
    {
        var request = new ParseChannelRequest
        {
            Channel = "superChannel",
            AlwaysCheckNewPosts = false,
            ScheduleId = Guid.NewGuid(),
            DateFrom = DateOnly.FromDateTime(DateTime.Now.AddDays(5)),
        };

        var createdSchedule = await client.PostAsync(Url, request.ToStringContent());
        createdSchedule.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WithDeleteMediaAndDeleteText_ShouldBadRequest()
    {
        var request = new ParseChannelRequest
        {
            Channel = "superChannel",
            AlwaysCheckNewPosts = false,
            ScheduleId = Guid.NewGuid(),
            DeleteMedia = true,
            DeleteText = true,
        };

        var createdSchedule = await client.PostAsync(Url, request.ToStringContent());
        createdSchedule.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WithInvalidScheduleId_ShouldBadRequest()
    {
        var request = new ParseChannelRequest
        {
            Channel = "superChannel",
            AlwaysCheckNewPosts = false,
            ScheduleId = Guid.Empty,
        };

        var createdSchedule = await client.PostAsync(Url, request.ToStringContent());
        createdSchedule.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}