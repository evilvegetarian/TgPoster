using System.Net;
using System.Net.Http.Json;
using Shouldly;
using TgPoster.API.Common;
using TgPoster.API.Domain.UseCases.TelegramBots.ListTelegramBot;
using TgPoster.API.Models;
using TgPoster.API.Tests.Helper;

namespace TgPoster.API.Tests.Endpoint;

public class TelegramBotEndpointTest(EndpointTestFixture fixture) : IClassFixture<EndpointTestFixture>
{
	private const string Url = Routes.TelegramBot.Root;
	private readonly HttpClient client = fixture.AuthClient;

	[Fact]
	public async Task Create_WithMissingToken_ShouldReturnBadRequest()
	{
		var request = new CreateTelegramBotRequest
		{
			Token = ""
		};
		var response = await client.PostAsJsonAsync(Url, request);
		response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task List_InitiallyEmpty_ThenContainsCreatedBot()
	{
		var listResponse = await client.GetAsync(Url);
		listResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
		var bots = await listResponse.Content.ReadFromJsonAsync<List<TelegramBotResponse>>();
		bots.ShouldNotBeNull();
		bots.Count.ShouldBeGreaterThan(0);
	}

	[Fact]
	public async Task List_WithAnotherUser_ShouldReturnEmptyList()
	{
		var response = await client.GetAsync<List<TelegramBotResponse>>(Url);
		response.ShouldNotBeNull();
		response.Count.ShouldBeGreaterThan(0);

		var anotherClient = fixture.GetClient(fixture.GenerateTestToken(GlobalConst.UserIdEmpty));

		var listAnotherResponse = await anotherClient.GetAsync<List<TelegramBotResponse>>(Url);
		listAnotherResponse!.Count.ShouldBeEquivalentTo(0);
	}

	[Fact]
	public async Task DeleteTelegramBot_WithNonExistingId_ShouldReturnNotFound()
	{
		var nonExistentId = Guid.NewGuid();
		var response = await client.DeleteAsync($"{Url}/{nonExistentId}");
		response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task DeleteTelegramBot_WithValidId_ShouldReturnOK()
	{
		var deleteResponse = await client.DeleteAsync(Url + "/" + GlobalConst.TelegramBotId);
		deleteResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

		var repeatDeleteResponse = await client.DeleteAsync(Url + "/" + GlobalConst.TelegramBotId);
		repeatDeleteResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
	}
}