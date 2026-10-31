using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using TgPoster.API.Common;
using TgPoster.API.Domain.Services;
using TgPoster.API.Domain.UseCases.YouTubeAccount.YouTubeAccountLogin;

namespace TgPoster.API.Tests.Endpoint;

public sealed class YouTubeCallbackAccessShould(AuthorizationTestFixture fixture) : IClassFixture<AuthorizationTestFixture>
{
	[Fact]
	public async Task SetBrowserBoundCookie_WhenAuthorizationStarts()
	{
		using var client = fixture.GetClient(AuthorizationTestFixture.OwnerId);
		using var form = new MultipartFormDataContent();
		form.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(
			"""{"web":{"client_id":"test-client","client_secret":"test-secret","redirect_uris":["https://example.test/callback"]}}""")),
			"JsonFile", "client.json");

		var response = await client.PostAsync(Routes.YouTubeAccount.Create, form);

		response.StatusCode.ShouldBe(HttpStatusCode.Created);
		var body = await response.Content.ReadFromJsonAsync<YouTubeAuthUrlResponse>();
		body.ShouldNotBeNull();
		var state = QueryHelpers.ParseQuery(new Uri(body.Url).Query)["state"].ToString();
		state.ShouldNotBe(AuthorizationTestFixture.YouTubeAccountId.ToString());
		var cookie = response.Headers.GetValues("Set-Cookie").Single();
		cookie.ShouldContain("tgposter-youtube-oauth=" + state);
		cookie.ShouldContain("httponly", Case.Insensitive);
		cookie.ShouldContain("secure", Case.Insensitive);
		cookie.ShouldContain("samesite=lax", Case.Insensitive);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("different-browser-state")]
	public async Task RejectCallbackWithoutConsumingState_WhenBrowserCookieDoesNotMatch(string? cookie)
	{
		var states = fixture.Services.GetRequiredService<YouTubeOAuthStateService>();
		var state = states.Create(AuthorizationTestFixture.YouTubeAccountId, AuthorizationTestFixture.OwnerId);
		using var client = fixture.GetClient();
		if (cookie is not null)
			client.DefaultRequestHeaders.Add("Cookie", "tgposter-youtube-oauth=" + cookie);

		var response = await client.GetAsync($"{Routes.YouTubeAccount.CallBack}?code=test-code&state={state}");

		response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
		states.Take(state).ShouldNotBeNull();
	}

	[Fact]
	public async Task RejectAccountIdAsState_WhenCallerTriesToChooseAccount()
	{
		var state = AuthorizationTestFixture.YouTubeAccountId.ToString("N");
		using var client = fixture.GetClient();
		client.DefaultRequestHeaders.Add("Cookie", "tgposter-youtube-oauth=" + state);

		var response = await client.GetAsync($"{Routes.YouTubeAccount.CallBack}?code=test-code&state={state}");

		response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task CheckOriginalOwnerAndRejectReplay_WhenCallbackUsesValidState()
	{
		var states = fixture.Services.GetRequiredService<YouTubeOAuthStateService>();
		var state = states.Create(AuthorizationTestFixture.YouTubeAccountId, AuthorizationTestFixture.OwnerId);
		fixture.YouTubeCallbacks.ClearReceivedCalls();
		using var client = fixture.GetClient(Guid.NewGuid());
		client.DefaultRequestHeaders.Add("Cookie", "tgposter-youtube-oauth=" + state);
		var url = $"{Routes.YouTubeAccount.CallBack}?code=test-code&state={state}";

		var first = await client.GetAsync(url);
		var replay = await client.GetAsync(url);

		first.StatusCode.ShouldBe(HttpStatusCode.NotFound);
		replay.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
		await fixture.YouTubeCallbacks.Received(1).GetClients(AuthorizationTestFixture.YouTubeAccountId,
			AuthorizationTestFixture.OwnerId, Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task RejectState_WhenExpired()
	{
		var states = fixture.Services.GetRequiredService<YouTubeOAuthStateService>();
		var state = states.Create(AuthorizationTestFixture.YouTubeAccountId, AuthorizationTestFixture.OwnerId);
		fixture.Clock.Now = fixture.Clock.Now.AddMinutes(11);
		using var client = fixture.GetClient();
		client.DefaultRequestHeaders.Add("Cookie", "tgposter-youtube-oauth=" + state);

		var response = await client.GetAsync($"{Routes.YouTubeAccount.CallBack}?code=test-code&state={state}");

		response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
	}
}
