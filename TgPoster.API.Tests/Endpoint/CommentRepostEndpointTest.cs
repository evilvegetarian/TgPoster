using System.Net;
using System.Net.Http.Json;
using Shouldly;
using TgPoster.API.Common;
using TgPoster.API.Domain.UseCases.CommentRepost.CreateCommentRepost;
using TgPoster.API.Domain.UseCases.CommentRepost.GetCommentRepost;
using TgPoster.API.Domain.UseCases.CommentRepost.ListCommentRepost;
using TgPoster.API.Models;
using TgPoster.API.Tests.Helper;

namespace TgPoster.API.Tests.Endpoint;

public sealed class CommentRepostEndpointTest(EndpointTestFixture fixture) : IClassFixture<EndpointTestFixture>
{
	private const string Url = Routes.CommentRepost.Root;
	private readonly HttpClient client = fixture.AuthClient;

	[Fact]
	public async Task Create_WithValidData_ShouldReturnCreated()
	{
		var request = new CreateCommentRepostRequest
		{
			ScheduleId = GlobalConst.Worked.ScheduleId,
			TelegramSessionId = GlobalConst.Worked.TelegramSessionId,
			WatchedChannel = GlobalConst.Worked.Channel
		};

		var response = await client.PostAsJsonAsync(Url, request);

		response.StatusCode.ShouldBe(HttpStatusCode.Created);
		var created = await response.ToObject<CreateCommentRepostResponse>();
		created.ShouldNotBeNull();
		created.Id.ShouldNotBe(Guid.Empty);
	}

	[Fact]
	public async Task Create_WithInvalidScheduleId_ShouldReturnNotFound()
	{
		var request = new CreateCommentRepostRequest
		{
			ScheduleId = Guid.NewGuid(),
			TelegramSessionId = GlobalConst.Worked.TelegramSessionId,
			WatchedChannel = GlobalConst.Worked.Channel
		};

		var response = await client.PostAsJsonAsync(Url, request);

		response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task Create_WithInvalidTelegramSessionId_ShouldReturnNotFound()
	{
		var request = new CreateCommentRepostRequest
		{
			ScheduleId = GlobalConst.Worked.ScheduleId,
			TelegramSessionId = Guid.NewGuid(),
			WatchedChannel = GlobalConst.Worked.Channel
		};

		var response = await client.PostAsJsonAsync(Url, request);

		response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task Get_WithValidId_ShouldReturnOk()
	{
		var created = await CreateCommentRepost();

		var response = await client.GetAsync($"{Url}/{created.Id}");

		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		var result = await response.ToObject<GetCommentRepostResponse>();
		result.ShouldNotBeNull();
		result.Id.ShouldBe(created.Id);
		result.ScheduleId.ShouldBe(GlobalConst.Worked.ScheduleId);
	}

	[Fact]
	public async Task Get_WithNonExistingId_ShouldReturnNotFound()
	{
		var response = await client.GetAsync($"{Url}/{Guid.NewGuid()}");

		response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task Get_WithAnotherUser_ShouldReturnNotFound()
	{
		var created = await CreateCommentRepost();

		var anotherClient = fixture.GetClient(fixture.GenerateTestToken(GlobalConst.UserIdEmpty));
		var response = await anotherClient.GetAsync($"{Url}/{created.Id}");

		response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task List_ShouldReturnList()
	{
		await CreateCommentRepost();

		var list = await client.GetAsync<ListCommentRepostResponse>(Url);

		list.ShouldNotBeNull();
		list.Items.ShouldNotBeEmpty();
	}

	[Fact]
	public async Task List_WithAnotherUser_ShouldReturnEmptyList()
	{
		await CreateCommentRepost();

		var anotherClient = fixture.GetClient(fixture.GenerateTestToken(GlobalConst.UserIdEmpty));
		var list = await anotherClient.GetAsync<ListCommentRepostResponse>(Url);

		list.Items.ShouldBeEmpty();
	}

	[Fact]
	public async Task Update_WithValidData_ShouldReturnNoContent()
	{
		var created = await CreateCommentRepost();
		var updateRequest = new UpdateCommentRepostRequest { IsActive = false };

		var response = await client.PutAsJsonAsync($"{Url}/{created.Id}", updateRequest);

		response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
	}

	[Fact]
	public async Task Update_WithNonExistingId_ShouldReturnNotFound()
	{
		var updateRequest = new UpdateCommentRepostRequest { IsActive = false };

		var response = await client.PutAsJsonAsync($"{Url}/{Guid.NewGuid()}", updateRequest);

		response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task Delete_WithExistingId_ShouldReturnNoContent()
	{
		var created = await CreateCommentRepost();

		var response = await client.DeleteAsync($"{Url}/{created.Id}");

		response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
	}

	[Fact]
	public async Task Delete_WithNonExistingId_ShouldReturnNotFound()
	{
		var response = await client.DeleteAsync($"{Url}/{Guid.NewGuid()}");

		response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task Delete_TwiceSameId_ShouldReturnNotFoundOnSecond()
	{
		var created = await CreateCommentRepost();

		var firstResponse = await client.DeleteAsync($"{Url}/{created.Id}");
		firstResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

		var secondResponse = await client.DeleteAsync($"{Url}/{created.Id}");
		secondResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
	}

	private async Task<CreateCommentRepostResponse> CreateCommentRepost()
	{
		var request = new CreateCommentRepostRequest
		{
			ScheduleId = GlobalConst.Worked.ScheduleId,
			TelegramSessionId = GlobalConst.Worked.TelegramSessionId,
			WatchedChannel = GlobalConst.Worked.Channel
		};

		return await client.PostAsync<CreateCommentRepostResponse>(Url, request);
	}
}
