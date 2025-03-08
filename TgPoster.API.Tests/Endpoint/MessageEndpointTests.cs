using System.Net;
using Shouldly;
using TgPoster.API.Common;
using TgPoster.API.Models;
using TgPoster.Domain.UseCases.Messages.ListMessage;
using TgPoster.Endpoint.Tests.Helper;

namespace TgPoster.Endpoint.Tests.Endpoint;

public class MessageEndpointTests(EndpointTestFixture fixture) : IClassFixture<EndpointTestFixture>
{
    private readonly HttpClient client = fixture.CreateClient();
    private readonly CreateHelper create = new(fixture.CreateClient());

    [Fact]
    public async Task CreateMessagesFromFiles_WithValidData_ReturnsCreated()
    {
        var files = FileHelper.GetIFormFilesFromDirectory();
        var request = new CreateMessagesFromFilesRequest
        {
            ScheduleId = GlobalConst.Worked.ScheduleId,
            Files = files
        };

        var createResponse = await client.PostAsync(Routes.Message.CreateMessagesFromFiles, request.ToMultipartForm());
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        
        var messages = await client.GetAsync<List<MessageResponse>>(Routes.Message.List + "?scheduleId=" + request.ScheduleId);
        messages.Count.ShouldBe(files.Count);
    }

    [Fact]
    public async Task CreateMessagesFromFiles_WithNonexistentScheduleId_ReturnsNotFound()
    {
        var request = new CreateMessagesFromFilesRequest
        {
            ScheduleId = Guid.Parse("715d41dd-0916-4878-9a5e-6d27cf6432f6"),
            Files = FileHelper.GetIFormFilesFromDirectory()
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
            Files = null
        };

        var response = await client.PostAsync(Routes.Message.CreateMessagesFromFiles, request.ToMultipartForm());
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ListMessages_WithNonexistentScheduleId_ReturnsNotFound()
    {
        var scheduleId = Guid.NewGuid();
        var response = await client.GetAsync(Routes.Message.List + "?scheduleId=" + scheduleId);
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}