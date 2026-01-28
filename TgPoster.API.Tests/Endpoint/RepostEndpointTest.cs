using System.Net;
using System.Net.Http.Json;
using Shouldly;
using TgPoster.API.Common;
using TgPoster.API.Domain.UseCases.Repost.AddRepostDestination;
using TgPoster.API.Domain.UseCases.Repost.CreateRepostSettings;
using TgPoster.API.Domain.UseCases.Repost.ListRepostSettings;
using TgPoster.API.Models;
using TgPoster.API.Tests.Helper;

namespace TgPoster.API.Tests.Endpoint;

//TODO:Переделать
/*public sealed class RepostEndpointTest(EndpointTestFixture fixture) : IClassFixture<EndpointTestFixture>
{
	private const string Url = Routes.Repost.Root;
	private readonly HttpClient client = fixture.AuthClient;

	[Fact]
	public async Task CreateSettings_WithValidData_ShouldReturnCreated()
	{
		var request = new CreateRepostSettingsRequest
		{
			ScheduleId = GlobalConst.Worked.ScheduleId,
			TelegramSessionId = GlobalConst.Worked.TelegramSessionId,
			Destinations = []
		};

		var response = await client.PostAsJsonAsync(Url + "/settings", request);

		response.StatusCode.ShouldBe(HttpStatusCode.Created);

		var createdSettings = await response.Content.ReadFromJsonAsync<CreateRepostSettingsResponse>();
		createdSettings.ShouldNotBeNull();
		createdSettings.Id.ShouldNotBe(Guid.Empty);
	}

	[Fact]
	public async Task CreateSettings_WithInvalidScheduleId_ShouldReturnNotFound()
	{
		var request = new CreateRepostSettingsRequest
		{
			ScheduleId = Guid.NewGuid(),
			TelegramSessionId = GlobalConst.Worked.TelegramSessionId,
			Destinations = []
		};

		var response = await client.PostAsJsonAsync(Url + "/settings", request);

		response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task CreateSettings_WithInvalidTelegramSessionId_ShouldReturnNotFound()
	{
		var request = new CreateRepostSettingsRequest
		{
			ScheduleId = GlobalConst.Worked.ScheduleId,
			TelegramSessionId = Guid.NewGuid(),
			Destinations = []
		};

		var response = await client.PostAsJsonAsync(Url + "/settings", request);

		response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task CreateSettings_DuplicateForSchedule_ShouldReturnBadRequest()
	{
		var request = new CreateRepostSettingsRequest
		{
			ScheduleId = GlobalConst.Worked.ScheduleId,
			TelegramSessionId = GlobalConst.Worked.TelegramSessionId,
			Destinations = []
		};

		var firstResponse = await client.PostAsJsonAsync(Url + "/settings", request);
		firstResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

		var secondResponse = await client.PostAsJsonAsync(Url + "/settings", request);
		secondResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task ListSettings_WithExistingSettings_ShouldReturnList()
	{
		var createRequest = new CreateRepostSettingsRequest
		{
			ScheduleId = GlobalConst.Worked.ScheduleId,
			TelegramSessionId = GlobalConst.Worked.TelegramSessionId,
			Destinations = []
		};

		await client.PostAsJsonAsync(Url + "/settings", createRequest);

		var getResponse = await client.GetAsync($"{Url}/settings");
		getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

		var response = await getResponse.Content.ReadFromJsonAsync<ListRepostSettingsResponse>();
		response.ShouldNotBeNull();
		response.Items.ShouldNotBeEmpty();
	}

	[Fact]
	public async Task DeleteSettings_WithExistingSettings_ShouldReturnNoContent()
	{
		var createRequest = new CreateRepostSettingsRequest
		{
			ScheduleId = GlobalConst.Worked.ScheduleId,
			TelegramSessionId = GlobalConst.Worked.TelegramSessionId,
			Destinations = []
		};

		var createResponse = await client.PostAsJsonAsync(Url + "/settings", createRequest);
		var createdSettings = await createResponse.Content.ReadFromJsonAsync<CreateRepostSettingsResponse>();

		var deleteResponse = await client.DeleteAsync($"{Url}/settings/{createdSettings!.Id}");

		deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);
	}

	[Fact]
	public async Task DeleteSettings_WithNonExistingSettings_ShouldReturnNotFound()
	{
		var nonExistingId = Guid.NewGuid();

		var response = await client.DeleteAsync($"{Url}/settings/{nonExistingId}");

		response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task AddDestination_WithValidData_ShouldReturnCreated()
	{
		var createSettingsRequest = new CreateRepostSettingsRequest
		{
			ScheduleId = GlobalConst.Worked.ScheduleId,
			TelegramSessionId = GlobalConst.Worked.TelegramSessionId,
			Destinations = []
		};

		var settingsResponse = await client.PostAsJsonAsync(Url + "/settings", createSettingsRequest);
		var settings = await settingsResponse.Content.ReadFromJsonAsync<CreateRepostSettingsResponse>();

		var addDestinationRequest = new AddRepostDestinationRequest
		{
			ChatIdentifier = GlobalConst.Worked.Channel
		};

		var response = await client.PostAsJsonAsync($"{Url}/settings/{settings!.Id}/destinations",
			addDestinationRequest);

		response.StatusCode.ShouldBe(HttpStatusCode.Created);

		var destination = await response.Content.ReadFromJsonAsync<AddRepostDestinationResponse>();
		destination.ShouldNotBeNull();
	}

	[Fact]
	public async Task AddDestination_WithNonExistingSettings_ShouldReturnNotFound()
	{
		var nonExistingSettingsId = Guid.NewGuid();
		var request = new AddRepostDestinationRequest
		{
			ChatIdentifier = "@channel1"
		};

		var response = await client.PostAsJsonAsync($"{Url}/settings/{nonExistingSettingsId}/destinations", request);

		response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task DeleteDestination_WithExistingDestination_ShouldReturnNoContent()
	{
		var createSettingsRequest = new CreateRepostSettingsRequest
		{
			ScheduleId = GlobalConst.Worked.ScheduleId,
			TelegramSessionId = GlobalConst.Worked.TelegramSessionId,
			Destinations = []
		};

		var settingsResponse = await client.PostAsJsonAsync(Url + "/settings", createSettingsRequest);
		var settings = await settingsResponse.Content.ReadFromJsonAsync<CreateRepostSettingsResponse>();

		var addDestinationRequest = new AddRepostDestinationRequest
		{
			ChatIdentifier = GlobalConst.Worked.Channel
		};

		var addResponse = await client.PostAsJsonAsync($"{Url}/settings/{settings!.Id}/destinations",
			addDestinationRequest);
		var destination = await addResponse.Content.ReadFromJsonAsync<AddRepostDestinationResponse>();

		var deleteResponse = await client.DeleteAsync($"{Url}/destinations/{destination!.Id}");

		deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);
	}

	[Fact]
	public async Task DeleteDestination_WithNonExistingDestination_ShouldReturnNotFound()
	{
		var nonExistingId = Guid.NewGuid();

		var response = await client.DeleteAsync($"{Url}/destinations/{nonExistingId}");

		response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task UpdateDestination_WithValidData_ShouldReturnNoContent()
	{
		var createSettingsRequest = new CreateRepostSettingsRequest
		{
			ScheduleId = GlobalConst.Worked.ScheduleId,
			TelegramSessionId = GlobalConst.Worked.TelegramSessionId,
			Destinations = []
		};

		var settings = await client.PostAsync<CreateRepostSettingsResponse>(Url + "/settings", createSettingsRequest);

		var addDestinationRequest = new AddRepostDestinationRequest
		{
			ChatIdentifier = GlobalConst.Worked.Channel
		};

		var addResponse = await client.PostAsJsonAsync($"{Url}/settings/{settings.Id}/destinations",
			addDestinationRequest);
		var destination = await addResponse.Content.ReadFromJsonAsync<AddRepostDestinationResponse>();

		var updateRequest = new UpdateRepostDestinationRequest
		{
			IsActive = false
		};

		var updateResponse = await client.PutAsJsonAsync($"{Url}/destinations/{destination!.Id}", updateRequest);

		updateResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);
	}

	[Fact]
	public async Task UpdateDestination_WithNonExistingDestination_ShouldReturnNotFound()
	{
		var nonExistingId = Guid.NewGuid();
		var request = new UpdateRepostDestinationRequest
		{
			IsActive = false
		};

		var response = await client.PutAsJsonAsync($"{Url}/destinations/{nonExistingId}", request);

		response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
	}
}*/
