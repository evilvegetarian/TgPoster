using System.Net;
using System.Net.Http.Json;
using Shouldly;
using TgPoster.API.Common;
using TgPoster.API.Domain.UseCases.Parse.ListChannel;
using TgPoster.API.Models;
using TgPoster.Endpoint.Tests.Helper;

namespace TgPoster.Endpoint.Tests.Endpoint;

public class ParseEndpointTest(EndpointTestFixture fixture) : IClassFixture<EndpointTestFixture>
{
    private const string Url = Routes.Parse.Root;
    private readonly HttpClient client = fixture.AuthClient;
    private readonly CreateHelper helper = new(fixture.AuthClient);

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
    public async Task Create_WithValidData_ShouldReturnCreated()
    {
        var request = new ParseChannelRequest
        {
            Channel = GlobalConst.Worked.Channel,
            AlwaysCheckNewPosts = false,
            ScheduleId = GlobalConst.Worked.ScheduleId,
        };

        var createdSchedule = await client.PostAsJsonAsync(Url, request);
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

    [Fact]
    public async Task Get_ShouldReturnValidData()
    {
        var request = new ParseChannelRequest
        {
            Channel = GlobalConst.Worked.Channel,
            ScheduleId = GlobalConst.Worked.ScheduleId,
            AlwaysCheckNewPosts = false,
            NeedVerifiedPosts = true,
            DeleteMedia = true,
            DeleteText = false
        };

        var createdSchedule = await client.PostAsJsonAsync(Url, request);
        createdSchedule.StatusCode.ShouldBe(HttpStatusCode.Created);

        var list = await client.GetFromJsonAsync<List<ParseChannelsResponse>>(Url);
        list!.Count.ShouldBeGreaterThan(0);
        list.ShouldNotBeNull();
        list.ShouldNotBeEmpty();
        list.ShouldContain(x =>
            x.NeedVerifiedPosts == request.NeedVerifiedPosts
            && x.DeleteMedia == request.DeleteMedia
            && x.DeleteText == request.DeleteText);
    }

    [Fact]
    public async Task Get_WithAnotherUser_ShouldReturnEmptyList()
    {
        var request = new ParseChannelRequest
        {
            Channel = GlobalConst.Worked.Channel,
            AlwaysCheckNewPosts = false,
            ScheduleId = GlobalConst.Worked.ScheduleId,
        };

        var createdSchedule = await client.PostAsJsonAsync(Url, request);
        createdSchedule.StatusCode.ShouldBe(HttpStatusCode.Created);

        var anotherClient = fixture.GetClient(fixture.GenerateTestToken(GlobalConst.UserIdEmpty));

        var list = await anotherClient.GetFromJsonAsync<List<ParseChannelsResponse>>(Url);
        list!.Count.ShouldBe(0);
    }
}