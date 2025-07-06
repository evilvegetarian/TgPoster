using System.Net;
using System.Net.Http.Json;
using Shouldly;
using TgPoster.API.Common;
using TgPoster.API.Domain.UseCases.Accounts.SignIn;
using TgPoster.API.Domain.UseCases.Accounts.SignOn;
using TgPoster.API.Models;

namespace TgPoster.Endpoint.Tests.Endpoint;

public class AccountEndpointTest(EndpointTestFixture fixture) : IClassFixture<EndpointTestFixture>
{
    private readonly HttpClient client = fixture.CreateClient();

    [Fact]
    public async Task SignOn_WithValidData_ShouldReturnUserId()
    {
        var request = new SignOnRequest
        {
            Login = "testuser1",
            Password = "testpassword"
        };
        var response = await client.PostAsJsonAsync(Routes.Account.SignOn, request);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<SignOnResponse>();
        result.ShouldNotBeNull();
        result.UserId.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task SignOn_WithExistingLogin_ShouldReturnBadRequest()
    {
        var request = new SignOnRequest
        {
            Login = "testuser2",
            Password = "testpassword"
        };
        // First registration
        var response1 = await client.PostAsJsonAsync(Routes.Account.SignOn, request);
        response1.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Second registration with same login
        var response2 = await client.PostAsJsonAsync(Routes.Account.SignOn, request);
        response2.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SignIn_WithValidCredentials_ShouldReturnTokens()
    {
        var login = "testuser3";
        var password = "testpassword";
        // Register user
        var signOnRequest = new SignOnRequest { Login = login, Password = password };
        var signOnResponse = await client.PostAsJsonAsync(Routes.Account.SignOn, signOnRequest);
        signOnResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Login
        var signInRequest = new SignInRequest { Login = login, Password = password };
        var signInResponse = await client.PostAsJsonAsync(Routes.Account.SignIn, signInRequest);
        signInResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await signInResponse.Content.ReadFromJsonAsync<SignInResponse>();
        result.ShouldNotBeNull();
        result.AccessToken.ShouldNotBeNullOrWhiteSpace();
        result.RefreshToken.ShouldNotBe(Guid.Empty);
        result.RefreshTokenExpiration.ShouldBeGreaterThan(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task SignIn_WithWrongPassword_ShouldReturnBadRequest()
    {
        var login = "testuser4";
        var password = "testpassword";
        // Register user
        var signOnRequest = new SignOnRequest { Login = login, Password = password };
        var signOnResponse = await client.PostAsJsonAsync(Routes.Account.SignOn, signOnRequest);
        signOnResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Login with wrong password
        var signInRequest = new SignInRequest { Login = login, Password = "wrongpassword" };
        var signInResponse = await client.PostAsJsonAsync(Routes.Account.SignIn, signInRequest);
        signInResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SignIn_WithNonExistentUser_ShouldReturnNotFound()
    {
        var signInRequest = new SignInRequest
        {
            Login = "nonexistentuser",
            Password = "anyPassword"
        };
        var signInResponse = await client.PostAsJsonAsync(Routes.Account.SignIn, signInRequest);
        signInResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
    
    [Fact]
    public async Task RefreshToken_WithNonExistentUser_ShouldReturnNotFound()
    {
        var signInRequest = new SignInRequest
        {
            Login = "nonexistentuser",
            Password = "anyPassword"
        };
        var signInResponse = await client.PostAsJsonAsync(Routes.Account.SignIn, signInRequest);
        signInResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}