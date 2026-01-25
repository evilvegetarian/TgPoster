using System.Net;
using System.Net.Http.Json;
using Shouldly;
using TgPoster.API.Common;
using TgPoster.API.Domain.UseCases.Parse.ListParseChannel;
using TgPoster.API.Models;
using TgPoster.API.Tests.Helper;

namespace TgPoster.API.Tests.Endpoint;

public class ParseChannelEndpointTest(EndpointTestFixture fixture) : IClassFixture<EndpointTestFixture>
{
	private const string Url = Routes.ParseChannel.Root;
	private readonly HttpClient client = fixture.AuthClient;
	private readonly CreateHelper helper = new(fixture.AuthClient);

	[Fact]
	public async Task Create_WithInValidChannel_ShouldBadRequest()
	{
		var request = new CreateParseChannelRequest
		{
			Channel = "",
			AlwaysCheckNewPosts = false,
			ScheduleId = Guid.NewGuid(),
			TelegramSessionId = GlobalConst.Worked.TelegramSessionId
		};

		var createdSchedule = await client.PostAsync(Url, request.ToStringContent());
		createdSchedule.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task Create_WithInValidDateFrom_ShouldBadRequest()
	{
		var request = new CreateParseChannelRequest
		{
			Channel = "superChannel",
			AlwaysCheckNewPosts = false,
			ScheduleId = Guid.NewGuid(),
			DateFrom = DateTime.Now.AddDays(5),
			TelegramSessionId = GlobalConst.Worked.TelegramSessionId
		};

		var createdSchedule = await client.PostAsync(Url, request.ToStringContent());
		createdSchedule.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task Create_WithDeleteMediaAndDeleteText_ShouldBadRequest()
	{
		var request = new CreateParseChannelRequest
		{
			Channel = "superChannel",
			AlwaysCheckNewPosts = false,
			ScheduleId = Guid.NewGuid(),
			DeleteMedia = true,
			DeleteText = true,
			TelegramSessionId = GlobalConst.Worked.TelegramSessionId
		};

		var createdSchedule = await client.PostAsync(Url, request.ToStringContent());
		createdSchedule.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task Create_WithValidData_ShouldReturnCreated()
	{
		var request = new CreateParseChannelRequest
		{
			Channel = GlobalConst.Worked.Channel,
			AlwaysCheckNewPosts = false,
			ScheduleId = GlobalConst.Worked.ScheduleId,
			TelegramSessionId = GlobalConst.Worked.TelegramSessionId
		};

		var createdSchedule = await client.PostAsJsonAsync(Url, request);
		createdSchedule.StatusCode.ShouldBe(HttpStatusCode.Created);
	}

	[Fact]
	public async Task Create_WithNotExistChannel_ShouldReturnBadRequest()
	{
		var request = new CreateParseChannelRequest
		{
			Channel = "GlobalConst.Worked.Channel",
			AlwaysCheckNewPosts = false,
			ScheduleId = GlobalConst.Worked.ScheduleId,
			TelegramSessionId = GlobalConst.Worked.TelegramSessionId
		};

		var createdSchedule = await client.PostAsync(Url, request.ToStringContent());
		createdSchedule.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task Create_WithNotExistSchedule_ShouldReturnNotFound()
	{
		var request = new CreateParseChannelRequest
		{
			Channel = GlobalConst.Worked.Channel,
			AlwaysCheckNewPosts = false,
			ScheduleId = Guid.Parse("78d09b44-2000-4579-89e7-def043aeab09"),
			TelegramSessionId = GlobalConst.Worked.TelegramSessionId
		};

		var createdSchedule = await client.PostAsync(Url, request.ToStringContent());
		createdSchedule.StatusCode.ShouldBe(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task Get_ShouldReturnValidData()
	{
		var request = new CreateParseChannelRequest
		{
			Channel = GlobalConst.Worked.Channel,
			ScheduleId = GlobalConst.Worked.ScheduleId,
			AlwaysCheckNewPosts = false,
			NeedVerifiedPosts = true,
			DeleteMedia = true,
			DeleteText = false,
			TelegramSessionId = GlobalConst.Worked.TelegramSessionId
		};

		var createdSchedule = await client.PostAsJsonAsync(Url, request);
		createdSchedule.StatusCode.ShouldBe(HttpStatusCode.Created);

		var list = await client.GetFromJsonAsync<ParseChannelListResponse>(Url);
		list.ShouldNotBeNull();
		list.Items.Count.ShouldBeGreaterThan(0);
		list.Items.ShouldNotBeEmpty();
		list.Items.ShouldContain(x =>
			x.NeedVerifiedPosts == request.NeedVerifiedPosts
			&& x.DeleteMedia == request.DeleteMedia
			&& x.DeleteText == request.DeleteText);
	}

	[Fact]
	public async Task Get_WithAnotherUser_ShouldReturnEmptyList()
	{
		var request = new CreateParseChannelRequest
		{
			Channel = GlobalConst.Worked.Channel,
			AlwaysCheckNewPosts = false,
			ScheduleId = GlobalConst.Worked.ScheduleId,
			TelegramSessionId = GlobalConst.Worked.TelegramSessionId
		};

		var createdSchedule = await client.PostAsJsonAsync(Url, request);
		createdSchedule.StatusCode.ShouldBe(HttpStatusCode.Created);

		var anotherClient = fixture.GetClient(fixture.GenerateTestToken(GlobalConst.UserIdEmpty));

		var list = await anotherClient.GetFromJsonAsync<ParseChannelListResponse>(Url);
		list.ShouldNotBeNull();
		list.Items.Count.ShouldBe(0);
	}

	[Fact]
	public async Task Update_WithAnotherUser_ShouldReturnNotFound()
	{
		var parseId = helper.CreateParseChannel();

		var anotherClient = fixture.GetClient(fixture.GenerateTestToken(GlobalConst.UserIdEmpty));

		var updateRequest = new UpdateParseChannelRequest
		{
			Channel = GlobalConst.Worked.Channel,
			ScheduleId = GlobalConst.Worked.ScheduleId,
			AlwaysCheckNewPosts = false,
			NeedVerifiedPosts = false,
			AvoidWords = ["cum"],
			TelegramSessionId = GlobalConst.Worked.TelegramSessionId
		};

		var updateResponse = await anotherClient.PutAsync(Url + "/" + parseId, updateRequest.ToStringContent());
		updateResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task Update_WithNonExistId_ShouldReturnNotFound()
	{
		var nonExistId = Guid.Parse("612db3c5-0f51-4e42-84f2-65d6a2ca1b3f");
		var updateRequest = new UpdateParseChannelRequest
		{
			Channel = GlobalConst.Worked.Channel,
			ScheduleId = GlobalConst.Worked.ScheduleId,
			AlwaysCheckNewPosts = false,
			NeedVerifiedPosts = false,
			AvoidWords = ["cum"],
			TelegramSessionId = GlobalConst.Worked.TelegramSessionId
		};

		var updateResponse = await client.PutAsync(Url + "/" + nonExistId, updateRequest.ToStringContent());
		updateResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task Update_WithValidData_ShouldOk()
	{
		var parseId = await helper.CreateParseChannel();
		var updateRequest = new UpdateParseChannelRequest
		{
			Channel = GlobalConst.Worked.Channel,
			ScheduleId = GlobalConst.Worked.ScheduleId,
			AlwaysCheckNewPosts = false,
			NeedVerifiedPosts = false,
			AvoidWords = ["cum"],
			TelegramSessionId = GlobalConst.Worked.TelegramSessionId
		};

		var updateResponse = await client.PutAsync(Url + "/" + parseId, updateRequest.ToStringContent());
		updateResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

		var list = await client.GetAsync<ParseChannelListResponse>(Url);
		var existParse = list.Items.FirstOrDefault(x => x.Id == parseId);
		existParse.ShouldNotBeNull();
		existParse.NeedVerifiedPosts.ShouldBe(updateRequest.NeedVerifiedPosts);
		existParse.Channel.ShouldBe(updateRequest.Channel);
		existParse.AvoidWords.ShouldBe(updateRequest.AvoidWords);
		existParse.DeleteMedia.ShouldBe(updateRequest.DeleteMedia);
		existParse.DateFrom.ShouldBe(updateRequest.DateFrom);
		existParse.DateTo.ShouldBe(updateRequest.DateTo);
		existParse.DeleteText.ShouldBe(updateRequest.DeleteText);
		existParse.ScheduleId.ShouldBe(updateRequest.ScheduleId);
	}

	[Fact]
	public async Task Delete_WithNonExitsId_ShouldReturnNotFound()
	{
		var notExistGuid = Guid.Parse("df317ad1-3959-4a2e-8183-d2df4f300932");
		var deleteResponse = await client.DeleteAsync(Url + $"/{notExistGuid}");
		deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task Delete_WithValidData_ShouldReturnOk()
	{
		var parseId = await helper.CreateParseChannel();

		var deleteResponse = await client.DeleteAsync(Url + $"/{parseId}");
		deleteResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

		var deleteResponse2 = await client.DeleteAsync(Url + $"/{parseId}");
		deleteResponse2.StatusCode.ShouldBe(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task Delete_WithAnotherUser_ShouldReturnNotFound()
	{
		var parseId = await helper.CreateParseChannel();

		var anotherClient = fixture.GetClient(fixture.GenerateTestToken(GlobalConst.UserIdEmpty));
		var deleteResponse = await anotherClient.DeleteAsync(Url + $"/{parseId}");
		deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
	}
}