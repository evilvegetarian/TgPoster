using System.Net;
using System.Net.Http.Json;
using Shouldly;
using TgPoster.API.Common;
using TgPoster.API.Models;
using TgPoster.Endpoint.Tests.Helper;
using TgPoster.API.Domain.UseCases.TelegramBots.CreateTelegramBot;
using TgPoster.API.Domain.UseCases.TelegramBots.ListTelegramBot;

namespace TgPoster.Endpoint.Tests.Endpoint;

public class TelegramBotEndpointTest(EndpointTestFixture fixture) : IClassFixture<EndpointTestFixture>
{
    private const string Url = Routes.TelegramBot.Root;
    private readonly HttpClient client = fixture.CreateClient();

    [Fact]
    public async Task CreateTelegramBot_WithMissingToken_ShouldReturnBadRequest()
    {
        var request = new CreateTelegramBotRequest
        {
            Token = ""
        };
        var response = await client.PostAsJsonAsync(Url, request);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ListTelegramBots_InitiallyEmpty_ThenContainsCreatedBot()
    {
        var listResponse = await client.GetAsync(Url);
        listResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var bots = await listResponse.Content.ReadFromJsonAsync<List<TelegramBotResponse>>();
        bots.ShouldNotBeNull();
        bots.Count.ShouldBeGreaterThan(0);
    }
}