using System.Net;
using System.Net.Http.Json;
using Shouldly;
using TgPoster.API.Common;
using TgPoster.API.Domain.UseCases.Schedules.CreateSchedule;
using TgPoster.API.Domain.UseCases.Schedules.ListSchedule;
using TgPoster.API.Models;
using TgPoster.API.Tests.Helper;

namespace TgPoster.API.Tests.Endpoint;

public class ScheduleEndpointTest(EndpointTestFixture fixture) : IClassFixture<EndpointTestFixture>
{
	private const string Url = Routes.Schedule.Root;
	private readonly HttpClient client = fixture.AuthClient;
	private readonly CreateHelper helper = new(fixture.AuthClient);

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
			Channel = "random-channel-not-exist"
		};

		var createdSchedule = await client.PostAsync(Url, request.ToStringContent());
		createdSchedule.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task Create_WithUserWithNotExistTelegram_ShouldNotFound()
	{
		var request = new CreateScheduleRequest
		{
			Name = "Test Schedule",
			TelegramBotId = GlobalConst.Worked.TelegramBotId,
			Channel = GlobalConst.Worked.Channel
		};
		var anotherClient = fixture.GetClient(fixture.GenerateTestToken(GlobalConst.UserIdEmpty));
		var createdSchedule = await anotherClient.PostAsync(Url, request.ToStringContent());
		createdSchedule.StatusCode.ShouldBe(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task List_ShouldReturnList()
	{
		var scheduleId = await helper.CreateSchedule();
		var schedules = await client.GetAsync<List<ScheduleResponse>>(Url);
		schedules.ShouldNotBeNull();
		schedules.ShouldNotBeEmpty();
		schedules.ShouldContain(x => x.Id == scheduleId);
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

	[Fact]
	public async Task Get_WithNonExistingId_ShouldReturnNotFound()
	{
		var nonExistentId = Guid.Parse("f383f214-97c5-4369-b708-f169dd5c9b6f");
		var response = await client.GetAsync(Url + "/" + nonExistentId);
		response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task Get_WithValidData_ShouldReturnOk()
	{
		var scheduleId = await helper.CreateSchedule();
		var response = await client.GetAsync(Url + "/" + scheduleId);
		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		var schedule = await response.Content.ReadFromJsonAsync<ScheduleResponse>();
		schedule.ShouldNotBeNull();
		schedule.Id.ShouldBe(scheduleId);
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
	public async Task Delete_WithAnotherUser_ShouldReturnNotFound()
	{
		var scheduleId = await helper.CreateSchedule();
		var anotherClient = fixture.GetClient(fixture.GenerateTestToken(GlobalConst.UserIdEmpty));
		var response = await anotherClient.DeleteAsync(Url + "/" + scheduleId);
		response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task UpdateStatus_WithAnotherUser_ShouldReturnNotFound()
	{
		var scheduleId = await helper.CreateSchedule();
		var anotherClient = fixture.GetClient(fixture.GenerateTestToken(GlobalConst.UserIdEmpty));
		var response = await anotherClient.PatchAsync(Url + "/" + scheduleId + "/status", null);
		response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task UpdateStatus_WithNotExistId_ShouldReturnNotFound()
	{
		var scheduleId = Guid.Parse("fa4cb02f-3efb-4f7c-b7db-2d8f53d10c2e");
		var response = await client.PatchAsync(Url + "/" + scheduleId + "/status", null);
		response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task UpdateStatus_ValidData_ShouldReturnOk()
	{
		//Изначальный статус true
		var initalStatus = true;
		var scheduleId = await helper.CreateSchedule();

		var response = await client.PatchAsync(Url + "/" + scheduleId + "/status", null);
		response.StatusCode.ShouldBe(HttpStatusCode.OK);

		var getResponse = await client.GetAsync<ScheduleResponse>(Url + "/" + scheduleId);
		getResponse.IsActive.ShouldBe(!initalStatus);
	}
}