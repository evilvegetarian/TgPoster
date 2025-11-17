using System.Net;
using Shouldly;
using TgPoster.API.Common;
using TgPoster.API.Models;
using TgPoster.Endpoint.Tests.Helper;

namespace TgPoster.Endpoint.Tests.Endpoint;

public class PromptSettingEndpointTest(EndpointTestFixture fixture) : IClassFixture<EndpointTestFixture>
{
	private const string Url = Routes.PromptSetting.Root;
	private readonly HttpClient client = fixture.AuthClient;
	private readonly CreateHelper helper = new(fixture.AuthClient);

	[Fact]
	public async Task CreatePromptSetting_WithNonExistScheduleId_ReturnsNotFound()
	{
		var request = new CreatePromptSettingRequest
		{
			PhotoPrompt = "",
			ScheduleId = Guid.NewGuid(),
		};
		var createResponse = await client.PostAsync(Url, request.ToStringContent());
		createResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task CreatePromptSetting_WithValidData_ReturnsCreated()
	{
		var scheduleId = await helper.CreateSchedule();
		var request = new CreatePromptSettingRequest
		{
			PhotoPrompt = "",
			ScheduleId = scheduleId,
		};
		var createResponse = await client.PostAsync(Url, request.ToStringContent());
		createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
	}
}
