using System.Net;
using System.Net.Http.Json;
using Shouldly;
using TgPoster.API.Common;
using TgPoster.API.Domain.UseCases.Repost.AddRepostDestination;
using TgPoster.API.Domain.UseCases.Repost.CreateRepostSettings;
using TgPoster.API.Domain.UseCases.Repost.GetRepostSettings;
using TgPoster.API.Domain.UseCases.Repost.ListRepostSettings;
using TgPoster.API.Domain.UseCases.Repost.SetRepostDestinationsActive;
using TgPoster.API.Models;
using TgPoster.API.Tests.Helper;

namespace TgPoster.API.Tests.Endpoint;

public sealed class RepostEndpointTest(EndpointTestFixture fixture) : IClassFixture<EndpointTestFixture>
{
	private const string SettingsUrl = Routes.Repost.CreateSettings;
	private const string DestinationsUrl = Routes.Repost.Root + "/destinations";
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

		var response = await client.PostAsJsonAsync(SettingsUrl, request);

		response.StatusCode.ShouldBe(HttpStatusCode.Created);

		var createdSettings = await response.ToObject<CreateRepostSettingsResponse>();
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

		var response = await client.PostAsJsonAsync(SettingsUrl, request);

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

		var response = await client.PostAsJsonAsync(SettingsUrl, request);

		response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task GetSettings_WithValidId_ShouldReturnOk()
	{
		var created = await CreateRepostSettings();

		var response = await client.GetAsync($"{SettingsUrl}/{created.Id}");

		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		var settings = await response.ToObject<RepostSettingsResponse>();
		settings.ShouldNotBeNull();
		settings.Id.ShouldBe(created.Id);
		settings.ScheduleId.ShouldBe(GlobalConst.Worked.ScheduleId);
	}

	[Fact]
	public async Task GetSettings_WithNonExistingId_ShouldReturnNotFound()
	{
		var response = await client.GetAsync($"{SettingsUrl}/{Guid.NewGuid()}");

		response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task GetSettings_WithAnotherUser_ShouldReturnNotFound()
	{
		var created = await CreateRepostSettings();

		var anotherClient = fixture.GetClient(fixture.GenerateTestToken(GlobalConst.UserIdEmpty));
		var response = await anotherClient.GetAsync($"{SettingsUrl}/{created.Id}");

		response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task ListSettings_ShouldReturnList()
	{
		await CreateRepostSettings();

		var response = await client.GetAsync(SettingsUrl);

		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		var list = await response.ToObject<ListRepostSettingsResponse>();
		list.ShouldNotBeNull();
		list.Items.ShouldNotBeEmpty();
	}

	[Fact]
	public async Task ListSettings_WithAnotherUser_ShouldReturnEmptyList()
	{
		await CreateRepostSettings();

		var anotherClient = fixture.GetClient(fixture.GenerateTestToken(GlobalConst.UserIdEmpty));
		var list = await anotherClient.GetAsync<ListRepostSettingsResponse>(SettingsUrl);

		list.Items.ShouldBeEmpty();
	}

	[Fact]
	public async Task UpdateSettings_WithValidData_ShouldReturnNoContent()
	{
		var created = await CreateRepostSettings();
		var updateRequest = new UpdateRepostSettingsRequest { IsActive = false };

		var response = await client.PutAsJsonAsync($"{SettingsUrl}/{created.Id}", updateRequest);

		response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
	}

	[Fact]
	public async Task UpdateSettings_WithNonExistingId_ShouldReturnNotFound()
	{
		var updateRequest = new UpdateRepostSettingsRequest { IsActive = false };

		var response = await client.PutAsJsonAsync($"{SettingsUrl}/{Guid.NewGuid()}", updateRequest);

		response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task UpdateSettings_WithDelayMaxLessThanMin_ShouldReturnBadRequest()
	{
		var created = await CreateRepostSettings();
		var updateRequest = new UpdateRepostSettingsRequest
		{
			IsActive = true,
			DefaultDelayMinSeconds = 60,
			DefaultDelayMaxSeconds = 10
		};

		var response = await client.PutAsJsonAsync($"{SettingsUrl}/{created.Id}", updateRequest);

		response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task UpdateSettings_WithNegativeSkipProbability_ShouldReturnBadRequest()
	{
		var created = await CreateRepostSettings();
		var updateRequest = new UpdateRepostSettingsRequest
		{
			IsActive = true,
			DefaultSkipProbability = -1
		};

		var response = await client.PutAsJsonAsync($"{SettingsUrl}/{created.Id}", updateRequest);

		response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task DeleteSettings_WithExistingSettings_ShouldReturnNoContent()
	{
		var created = await CreateRepostSettings();

		var response = await client.DeleteAsync($"{SettingsUrl}/{created.Id}");

		response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
	}

	[Fact]
	public async Task DeleteSettings_WithNonExistingSettings_ShouldReturnNotFound()
	{
		var response = await client.DeleteAsync($"{SettingsUrl}/{Guid.NewGuid()}");

		response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task DeleteSettings_TwiceSameId_ShouldReturnNotFoundOnSecond()
	{
		var created = await CreateRepostSettings();

		var firstResponse = await client.DeleteAsync($"{SettingsUrl}/{created.Id}");
		firstResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

		var secondResponse = await client.DeleteAsync($"{SettingsUrl}/{created.Id}");
		secondResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task AddDestination_WithValidData_ShouldReturnCreated()
	{
		var created = await CreateRepostSettings();
		var request = new AddRepostDestinationRequest
		{
			ChatIdentifier = GlobalConst.Worked.Channel
		};

		var response = await client.PostAsJsonAsync($"{SettingsUrl}/{created.Id}/destinations", request);

		response.StatusCode.ShouldBe(HttpStatusCode.Created);
		var destination = await response.ToObject<AddRepostDestinationResponse>();
		destination.ShouldNotBeNull();
		destination.Id.ShouldNotBe(Guid.Empty);
	}

	[Fact]
	public async Task AddDestination_WithNonExistingSettings_ShouldReturnNotFound()
	{
		var request = new AddRepostDestinationRequest
		{
			ChatIdentifier = GlobalConst.Worked.Channel
		};

		var response = await client.PostAsJsonAsync($"{SettingsUrl}/{Guid.NewGuid()}/destinations", request);

		response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task DeleteDestination_WithExistingDestination_ShouldReturnNoContent()
	{
		var destination = await CreateRepostSettingsWithDestination();

		var response = await client.DeleteAsync($"{DestinationsUrl}/{destination.Id}");

		response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
	}

	[Fact]
	public async Task DeleteDestination_WithNonExistingId_ShouldReturnNotFound()
	{
		var response = await client.DeleteAsync($"{DestinationsUrl}/{Guid.NewGuid()}");

		response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task UpdateDestination_WithValidData_ShouldReturnNoContent()
	{
		var destination = await CreateRepostSettingsWithDestination();
		var updateRequest = new UpdateRepostDestinationRequest
		{
			IsActive = false,
			DelayMinSeconds = 10,
			DelayMaxSeconds = 60,
			RepostEveryNth = 2,
			SkipProbability = 25,
			MaxRepostsPerDay = 50
		};

		var response = await client.PutAsJsonAsync($"{DestinationsUrl}/{destination.Id}", updateRequest);

		response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
	}

	[Fact]
	public async Task UpdateDestination_WithNonExistingId_ShouldReturnNotFound()
	{
		var updateRequest = new UpdateRepostDestinationRequest
		{
			IsActive = false,
			RepostEveryNth = 1
		};

		var response = await client.PutAsJsonAsync($"{DestinationsUrl}/{Guid.NewGuid()}", updateRequest);

		response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task SetDestinationsActive_DisableThenEnable_ShouldSwitchDestination()
	{
		var (settingsId, destination) = await CreateSettingsAndDestination();

		var disabled = await SetDestinationsActive(settingsId, [destination.Id], false);
		var enabled = await SetDestinationsActive(settingsId, [destination.Id], true);

		disabled.UpdatedCount.ShouldBe(1);
		enabled.UpdatedCount.ShouldBe(1);
		enabled.NotMemberCount.ShouldBe(0);
		enabled.NoPermissionCount.ShouldBe(0);
	}

	[Fact]
	public async Task SetDestinationsActive_WithNonExistingSettings_ShouldReturnNotFound()
	{
		var request = new SetRepostDestinationsActiveRequest { DestinationIds = [Guid.NewGuid()], IsActive = false };

		var response = await client.PutAsJsonAsync(DestinationsActiveUrl(Guid.NewGuid()), request);

		response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task SetDestinationsActive_WithEmptyList_ShouldReturnBadRequest()
	{
		var settings = await CreateRepostSettings();
		var request = new SetRepostDestinationsActiveRequest { DestinationIds = [], IsActive = false };

		var response = await client.PutAsJsonAsync(DestinationsActiveUrl(settings.Id), request);

		response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task DeleteDestinations_WithOwnDestination_ShouldReturnNoContent()
	{
		var (settingsId, destination) = await CreateSettingsAndDestination();
		var request = new DeleteRepostDestinationsRequest { DestinationIds = [destination.Id] };

		var response = await client.PostAsJsonAsync(DeleteDestinationsUrl(settingsId), request);

		response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
	}

	[Fact]
	public async Task DeleteDestinations_WithDestinationOfOtherSettings_ShouldReturnNotFound()
	{
		var (_, destination) = await CreateSettingsAndDestination();
		var otherSettings = await CreateRepostSettings();
		var request = new DeleteRepostDestinationsRequest { DestinationIds = [destination.Id] };

		var response = await client.PostAsJsonAsync(DeleteDestinationsUrl(otherSettings.Id), request);

		response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
	}

	private static string DestinationsActiveUrl(Guid settingsId) => $"{SettingsUrl}/{settingsId}/destinations/active";

	private static string DeleteDestinationsUrl(Guid settingsId) => $"{SettingsUrl}/{settingsId}/destinations/delete";

	private async Task<SetRepostDestinationsActiveResponse> SetDestinationsActive(
		Guid settingsId,
		List<Guid> destinationIds,
		bool isActive
	)
	{
		var request = new SetRepostDestinationsActiveRequest { DestinationIds = destinationIds, IsActive = isActive };
		var response = await client.PutAsJsonAsync(DestinationsActiveUrl(settingsId), request);
		response.StatusCode.ShouldBe(HttpStatusCode.OK);

		var result = await response.ToObject<SetRepostDestinationsActiveResponse>();
		result.ShouldNotBeNull();
		return result;
	}

	private async Task<(Guid SettingsId, AddRepostDestinationResponse Destination)> CreateSettingsAndDestination()
	{
		var settings = await CreateRepostSettings();
		var request = new AddRepostDestinationRequest
		{
			ChatIdentifier = GlobalConst.Worked.Channel
		};

		var destination = await client.PostAsync<AddRepostDestinationResponse>(
			$"{SettingsUrl}/{settings.Id}/destinations", request);

		return (settings.Id, destination);
	}

	private async Task<CreateRepostSettingsResponse> CreateRepostSettings()
	{
		var request = new CreateRepostSettingsRequest
		{
			ScheduleId = GlobalConst.Worked.ScheduleId,
			TelegramSessionId = GlobalConst.Worked.TelegramSessionId,
			Destinations = []
		};

		return await client.PostAsync<CreateRepostSettingsResponse>(SettingsUrl, request);
	}

	private async Task<AddRepostDestinationResponse> CreateRepostSettingsWithDestination() =>
		(await CreateSettingsAndDestination()).Destination;
}