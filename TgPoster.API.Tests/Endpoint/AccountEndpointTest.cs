using System.Net;
using System.Net.Http.Json;
using Shouldly;
using TgPoster.API.Common;
using TgPoster.API.Domain.UseCases.Accounts.SignIn;
using TgPoster.API.Domain.UseCases.Accounts.SignOn;
using TgPoster.API.Models;
using TgPoster.API.Tests.Helper;

namespace TgPoster.API.Tests.Endpoint;

public class AccountEndpointTest(EndpointTestFixture fixture) : IClassFixture<EndpointTestFixture>
{
	private readonly HttpClient client = fixture.CreateClient();
	private readonly CreateHelper helper = new(fixture.CreateClient());

	[Fact]
	public async Task SignOn_WithValidData_ShouldReturnUserId()
	{
		var request = new SignOnRequest
		{
			Login = "testuser1",
			Password = "testpassword"
		};
		var response = await client.PostAsync<SignOnResponse>(Routes.Account.SignOn, request);
		response.ShouldNotBeNull();
		response.UserId.ShouldNotBe(Guid.Empty);
	}

	[Fact]
	public async Task SignOn_WithExistingLogin_ShouldReturnBadRequest()
	{
		var request = new SignOnRequest
		{
			Login = "superPerfectLogin",
			Password = "superPerfectPassword"
		};

		await helper.SignOn(request.Login, request.Password);

		var response2 = await client.PostAsJsonAsync(Routes.Account.SignOn, request);
		response2.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task SignIn_WithValidCredentials_ShouldReturnTokens()
	{
		var login = "testuser3";
		var password = "testpassword";
		await helper.SignOn(login, password);

		var signInRequest = new SignInRequest { Login = login, Password = password };
		var signInResponse = await client.PostAsync<SignInResponse>(Routes.Account.SignIn, signInRequest);
		signInResponse.ShouldNotBeNull();
		signInResponse.AccessToken.ShouldNotBeNullOrWhiteSpace();
		signInResponse.RefreshToken.ShouldNotBe(Guid.Empty);
		signInResponse.RefreshTokenExpiration.ShouldBeGreaterThan(DateTimeOffset.UtcNow);
	}

	[Fact]
	public async Task SignIn_WithWrongPassword_ShouldReturnBadRequest()
	{
		var login = "testuser4";
		var password = "testpassword";
		await helper.SignOn(login, password);

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
	public async Task RefreshToken_WithNonExistRefresh_ShouldReturnNotFound()
	{
		var refreshTokenRequest = new RefreshTokenRequest
		{
			RefreshToken = Guid.Parse("44448354-3701-4a64-b2cf-b661d77f0f61")
		};
		var signInResponse = await client.PostAsJsonAsync(Routes.Account.RefreshToken, refreshTokenRequest);
		signInResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
	}


	[Fact]
	public async Task RefreshToken_WithValidData_ShouldReturnNewTokens()
	{
		var login = "testus85er4";
		var password = "testpassword";
		await helper.SignOn(login, password);
		var signInResponse = await helper.SignIn(login, password);

		var refreshTokenRequest = new RefreshTokenRequest
		{
			RefreshToken = signInResponse.RefreshToken
		};
		var refreshResponse = await client.PostAsJsonAsync(Routes.Account.RefreshToken, refreshTokenRequest);
		refreshResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
	}
}