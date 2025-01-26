using System.Net;
using Shouldly;
using TgPoster.API.Common;
using TgPoster.API.Models;

namespace TgPoster.E2E;

public class AccountEndpointsShould(EndpointTestFixture fixture) : IClassFixture<EndpointTestFixture>
{
    [Fact]
    public async Task SignInAfterSignOn()
    {
        var client = fixture.CreateClient();
        var request = new SignOnRequest
        {
            Login = "testUser",
            Password = "testPassword"
        };
        var response = await client.PostAsync(Routes.Account.SignOn, HttpContentHelper.GetJsonContent(request));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var responseString = await response.Content.ReadAsStringAsync();
        var isValid = Guid.TryParse(responseString, out var userId);
        isValid.ShouldBeTrue();
    }
}