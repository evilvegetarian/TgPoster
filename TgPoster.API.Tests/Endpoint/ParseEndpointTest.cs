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
            DateFrom = DateTime.Now.AddDays(5)
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
            DeleteText = true
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
            ScheduleId = Guid.Empty
        };

        var createdSchedule = await client.PostAsync(Url, request.ToStringContent());
        createdSchedule.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WithValidData_ShouldReturnCreated()
    {
        var request = new ParseChannelRequest
        {
            Channel = GlobalConst.Worked.Channel,
            AlwaysCheckNewPosts = false,
            ScheduleId = GlobalConst.Worked.ScheduleId
        };

        var createdSchedule = await client.PostAsync(Url, request.ToStringContent());
        var ss = await createdSchedule.Content.ReadAsStringAsync();
        createdSchedule.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Create_WithNotExistChannel_ShouldReturnBadRequest()
    {
        var request = new ParseChannelRequest
        {
            Channel = "GlobalConst.Worked.Channel",
            AlwaysCheckNewPosts = false,
            ScheduleId = GlobalConst.Worked.ScheduleId
        };

        var createdSchedule = await client.PostAsync(Url, request.ToStringContent());
        createdSchedule.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WithNotExistSchedule_ShouldReturnNotFound()
    {
        var request = new ParseChannelRequest
        {
            Channel = GlobalConst.Worked.Channel,
            AlwaysCheckNewPosts = false,
            ScheduleId = Guid.Parse("78d09b44-2000-4579-89e7-def043aeab09")
        };

        var createdSchedule = await client.PostAsync(Url, request.ToStringContent());
        createdSchedule.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}