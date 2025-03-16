using System.Net;
using Shouldly;
using TgPoster.API.Common;
using TgPoster.API.Models;
using TgPoster.Domain.UseCases.Messages.CreateMessage;
using TgPoster.Domain.UseCases.Messages.ListMessage;
using TgPoster.Endpoint.Tests.Helper;

namespace TgPoster.Endpoint.Tests.Endpoint;

public class MessageEndpointTests(EndpointTestFixture fixture) : IClassFixture<EndpointTestFixture>
{
    private readonly HttpClient client = fixture.CreateClient();
    private readonly CreateHelper create = new(fixture.CreateClient());
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
    public async Task CreateMessagesFromFiles_WithNonexistentScheduleId_ReturnsNotFound()
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
        var scheduleId = await create.CreateSchedule();
        var request = new CreateMessagesFromFilesRequest
        {
            ScheduleId = scheduleId,
            Files = null!
        };

        var response = await client.PostAsync(Routes.Message.CreateMessagesFromFiles, request.ToMultipartForm());
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ListMessages_WithNonExistentScheduleId_ReturnsNotFound()
    {
        var scheduleId = Guid.NewGuid();
        var response = await client.GetAsync(Routes.Message.List + "?scheduleId=" + scheduleId);
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetMessage_WithNonExistentId_ReturnsNotFound()
    {
        var nonExistMessageId = Guid.NewGuid();
        var response = await client.GetAsync(Url + "/" + nonExistMessageId);
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateMessage_WithValidDate_ReturnsCreated()
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
    public async Task CreateMessage_WithInvalidTimePosting_ReturnsBadRequest()
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
    public async Task CreateMessage_WithNonExistScheduleId_ReturnsNotFound()
    {
        var createMessage = new CreateMessageRequest
        {
            ScheduleId = Guid.NewGuid(),
            TimePosting = DateTimeOffset.Now.AddMinutes(5)
        };
        var response = await client.PostAsync(Url, createMessage.ToMultipartForm());
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}