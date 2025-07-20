using System.Net;
using System.Net.Http.Json;
using Shouldly;
using TgPoster.API.Common;
using TgPoster.API.Domain.UseCases.Messages.CreateMessage;
using TgPoster.API.Domain.UseCases.Messages.ListMessage;
using TgPoster.API.Models;
using TgPoster.Endpoint.Tests.Helper;

namespace TgPoster.Endpoint.Tests.Endpoint;

public class MessageEndpointTests(EndpointTestFixture fixture) : IClassFixture<EndpointTestFixture>
{
    private readonly HttpClient client = fixture.AuthClient;
    private readonly CreateHelper helper = new(fixture.AuthClient);
    private readonly string Url = Routes.Message.Root;

    [Fact]
    public async Task CreateMessagesFromFiles_WithValidData_ReturnsCreated()
    {
        var files = FileHelper.GetTestIFormFiles();
        var request = new CreateMessagesFromFilesRequest
        {
            ScheduleId = GlobalConst.Worked.ScheduleId,
            Files = files
        };

        var createResponse = await client.PostAsync(Routes.Message.CreateMessagesFromFiles, request.ToMultipartForm());
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var messages =
            await client.GetAsync<List<MessageResponse>>(Routes.Message.List + "?scheduleId=" + request.ScheduleId);
        messages.Count.ShouldBe(files.Count);
    }

    [Fact]
    public async Task CreateMessagesFromFiles_WithNonExistScheduleId_ReturnNotFound()
    {
        var request = new CreateMessagesFromFilesRequest
        {
            ScheduleId = Guid.Parse("715d41dd-0916-4878-9a5e-6d27cf6432f6"),
            Files = FileHelper.GetTestIFormFiles()
        };

        var response = await client.PostAsync(Routes.Message.CreateMessagesFromFiles, request.ToMultipartForm());
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateMessagesFromFiles_WithInvalidData_ReturnsBadRequest()
    {
        var scheduleId = await helper.CreateSchedule();
        var request = new CreateMessagesFromFilesRequest
        {
            ScheduleId = scheduleId,
            Files = null!
        };

        var response = await client.PostAsync(Routes.Message.CreateMessagesFromFiles, request.ToMultipartForm());
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateMessagesFromFiles_WithAnotherUser_ReturnsNotFound()
    {
        var files = FileHelper.GetTestIFormFiles();
        var request = new CreateMessagesFromFilesRequest
        {
            ScheduleId = GlobalConst.Worked.ScheduleId,
            Files = files
        };

        var anotherClient = fixture.GetClient(fixture.GenerateTestToken(GlobalConst.UserIdEmpty));
        var createResponse =
            await anotherClient.PostAsync(Routes.Message.CreateMessagesFromFiles, request.ToMultipartForm());
        createResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task List_WithNonExistentScheduleId_ReturnsNotFound()
    {
        var scheduleId = Guid.NewGuid();
        var response = await client.GetAsync(Routes.Message.List + "?scheduleId=" + scheduleId);
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task List_WithValidData_ReturnsOk()
    {
        var scheduleId = await helper.CreateSchedule();
        await helper.CreateMessages(scheduleId);

        var response = await client.GetAsync(Routes.Message.List + "?scheduleId=" + scheduleId);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var messages = await response.Content.ReadFromJsonAsync<List<MessageResponse>>();
        messages!.Count.ShouldBeGreaterThan(0);
        messages.ShouldNotBeNull();
    }

    [Fact]
    public async Task List_WithAnotherUser_ReturnsNotFound()
    {
        var scheduleId = await helper.CreateSchedule();
        await helper.CreateMessages(scheduleId);

        var anotherClient = fixture.GetClient(fixture.GenerateTestToken(GlobalConst.UserIdEmpty));
        var response = await anotherClient.GetAsync(Routes.Message.List + "?scheduleId=" + scheduleId);
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task List_WithNonExistentId_ReturnsNotFound()
    {
        var nonExistMessageId = Guid.NewGuid();
        var response = await client.GetAsync(Url + "/" + nonExistMessageId);
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task List_WithAnotherUser_ShouldReturnEmptyList()
    {
        var createMessage = new CreateMessageRequest
        {
            ScheduleId = GlobalConst.Worked.ScheduleId,
            TimePosting = DateTimeOffset.UtcNow.AddDays(1)
        };
        var createdResponse = await client.PostAsync(Url, createMessage.ToMultipartForm());
        createdResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var listResponseMessage = await client.GetAsync(Url + "?scheduleId=" + createMessage.ScheduleId);
        listResponseMessage.StatusCode.ShouldBe(HttpStatusCode.OK);

        var anotherClient = fixture.GetClient(fixture.GenerateTestToken(GlobalConst.UserIdEmpty));

        var listAnotherResponse = await anotherClient.GetAsync(Url + "?scheduleId=" + createMessage.ScheduleId);
        listAnotherResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_WithValidDate_ReturnsCreated()
    {
        var createMessage = new CreateMessageRequest
        {
            ScheduleId = GlobalConst.Worked.ScheduleId,
            TimePosting = DateTimeOffset.UtcNow.AddDays(1),
            Files = [FileHelper.GetTestIFormFile()]
        };

        var response = await client.PostMultipartFormAsync<CreateMessageResponse>(Url, createMessage);
        var getResponse = await client.GetAsync<MessageResponse>(Url + "/" + response.Id);

        getResponse.ScheduleId.ShouldBe(createMessage.ScheduleId);
        getResponse.TextMessage.ShouldBe(createMessage.TextMessage);
        getResponse.Files.Count.ShouldBe(createMessage.Files.Count);
    }

    [Fact]
    public async Task Create_WithInvalidTimePosting_ReturnsBadRequest()
    {
        var createMessage = new CreateMessageRequest
        {
            ScheduleId = GlobalConst.Worked.ScheduleId,
            TimePosting = DateTimeOffset.Now.AddMinutes(-1)
        };
        var response = await client.PostAsync(Url, createMessage.ToMultipartForm());
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WithNonExistScheduleId_ReturnsNotFound()
    {
        var createMessage = new CreateMessageRequest
        {
            ScheduleId = Guid.NewGuid(),
            TimePosting = DateTimeOffset.Now.AddMinutes(5)
        };
        var response = await client.PostAsync(Url, createMessage.ToMultipartForm());
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_WithAnotherUser_ShouldReturnNotFound()
    {
        var createMessage = new CreateMessageRequest
        {
            ScheduleId = GlobalConst.Worked.ScheduleId,
            TimePosting = DateTimeOffset.UtcNow.AddDays(1)
        };
        var createdResponse = await client.PostMultipartFormAsync<CreateMessageResponse>(Url, createMessage);

        var listResponseMessage = await client.GetAsync(Url + "/" + createdResponse.Id);
        listResponseMessage.StatusCode.ShouldBe(HttpStatusCode.OK);

        var anotherClient = fixture.GetClient(fixture.GenerateTestToken(GlobalConst.UserIdEmpty));

        var listAnotherResponse = await anotherClient.GetAsync(Url + "/" + createdResponse.Id);
        listAnotherResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}