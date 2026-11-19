using System.Net;
using System.Net.Http.Json;
using System.Text;
using Google;
using Google.Apis.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Security.IdentityServices;
using Shouldly;
using TgPoster.API.Domain.Services;
using TgPoster.API.Domain.UseCases.YouTubeAccount.CallBackYouTube;
using TgPoster.API.Domain.UseCases.YouTubeAccount.YouTubeAccountLogin;
using TgPoster.Exceptions.BadRequest;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.Tests.YouTubeAccount;

public sealed class YouTubeAuthorizationShould : IDisposable
{
	private readonly MemoryCache cache = new(new MemoryCacheOptions());
	private readonly Guid accountId = Guid.NewGuid();
	private readonly Guid userId = Guid.NewGuid();
	private readonly Mock<ICallBackYouTubeStorage> storage = new();
	private readonly OAuthHandler handler = new();
	private readonly YouTubeOAuthStateService states;
	private readonly CallBackYouTubeUseCase sut;

	public YouTubeAuthorizationShould()
	{
		states = new(cache, TimeProvider.System);
		storage.Setup(x => x.GetClients(accountId, userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(("test-client", "test-secret"));
		sut = new(storage.Object, states, new TestHttpClientFactory(handler));
	}

	public void Dispose() { handler.Dispose(); cache.Dispose(); }

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task BuildAuthorizationUrl_WithScopesAndStateBoundToAccountAndOwner(bool jsonFile)
	{
		var create = new Mock<ILoginYouTubeStorage>();
		create.Setup(x => x.CreateYouTubeAccountAsync("", "test-client", "test-secret", userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(accountId);
		var identity = new Mock<IIdentityProvider>();
		identity.SetupGet(x => x.Current).Returns(new Identity(userId));
		using var json = new MemoryStream(Encoding.UTF8.GetBytes("""
			{"web":{"client_id":"test-client","client_secret":"test-secret"}}
			"""));
		var file = jsonFile ? new FormFile(json, 0, json.Length, "JsonFile", "oauth.json") : null;
		var login = new LoginYouTubeUseCase(create.Object, identity.Object, states);

		var result = await login.Handle(new LoginYouTubeCommand(file, jsonFile ? null : "test-client",
			jsonFile ? null : "test-secret", "https://client.example.test/callback"), CancellationToken.None);

		var url = new Uri(result.Url);
		url.Host.ShouldBe("accounts.google.com");
		var query = QueryHelpers.ParseQuery(url.Query);
		query["client_id"].ToString().ShouldBe("test-client");
		query["redirect_uri"].ToString().ShouldBe("https://client.example.test/callback");
		query["scope"].ToString().ShouldContain("youtube.upload");
		query["scope"].ToString().ShouldContain("youtube.readonly");
		var state = states.Take(query["state"].ToString());
		state.ShouldNotBeNull();
		state.AccountId.ShouldBe(accountId);
		state.UserId.ShouldBe(userId);
		create.VerifyAll();
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("different-cookie-state")]
	public async Task RejectMissingOrMismatchedCookieState_BeforeAccessingStorage(string? expected)
	{
		var state = states.Create(accountId, userId);

		await Should.ThrowAsync<InvalidYouTubeOAuthStateException>(() => sut.Handle(
			new("code", state, "https://client.example.test/callback", expected), CancellationToken.None));

		storage.VerifyNoOtherCalls();
		handler.Requests.ShouldBe(0);
		states.Take(state).ShouldNotBeNull();
	}

	[Fact]
	public async Task RejectUnknownState_BeforeAccessingStorage()
	{
		var unknown = Guid.NewGuid().ToString("N");

		await Should.ThrowAsync<InvalidYouTubeOAuthStateException>(() => sut.Handle(
			new("code", unknown, "https://client.example.test/callback", unknown), CancellationToken.None));

		storage.VerifyNoOtherCalls();
		handler.Requests.ShouldBe(0);
	}

	[Theory]
	[InlineData("", "test-secret")]
	[InlineData("test-client", "")]
	public async Task RejectMissingAccountCredentials_WithoutCallingGoogle(string client, string secret)
	{
		storage.Setup(x => x.GetClients(accountId, userId, It.IsAny<CancellationToken>())).ReturnsAsync((client, secret));
		var state = states.Create(accountId, userId);

		await Should.ThrowAsync<YouTubeAccountNotFoundException>(() => sut.Handle(
			new("code", state, "https://client.example.test/callback", state), CancellationToken.None));

		handler.Requests.ShouldBe(0);
		storage.Verify(x => x.AddToken(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(),
			It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task ExchangeCodeAndSaveTokensAndChannel_AndPreventStateReuse(bool emptyChannel)
	{
		handler.EmptyChannel = emptyChannel;
		var state = states.Create(accountId, userId);
		var request = new CallBackYouTubeQuery("auth-code", state, "https://client.example.test/callback", state);

		await sut.Handle(request, CancellationToken.None);

		handler.TokenRequest.ShouldContain("code=auth-code");
		handler.TokenRequest.ShouldContain("client_id=test-client");
		handler.ChannelQuery.ShouldContain("mine=true");
		storage.Verify(x => x.AddToken(accountId, userId, "callback-access", "callback-refresh",
			emptyChannel ? "Unknown Channel" : "My Channel", emptyChannel ? null : "channel-id", CancellationToken.None), Times.Once);
		await Should.ThrowAsync<InvalidYouTubeOAuthStateException>(() => sut.Handle(request, CancellationToken.None));
		handler.Requests.ShouldBe(2);
	}

	[Fact]
	public async Task NotSaveTokens_WhenGoogleRejectsTheAuthorizationCode()
	{
		handler.RejectToken = true;
		var state = states.Create(accountId, userId);

		await Should.ThrowAsync<Google.Apis.Auth.OAuth2.Responses.TokenResponseException>(() => sut.Handle(
			new("invalid-code", state, "https://client.example.test/callback", state), CancellationToken.None));

		handler.Requests.ShouldBe(1);
		storage.Verify(x => x.AddToken(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(),
			It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task NotSaveTokens_WhenChannelLookupFails()
	{
		handler.RejectChannel = true;
		var state = states.Create(accountId, userId);

		await Should.ThrowAsync<GoogleApiException>(() => sut.Handle(
			new("code", state, "https://client.example.test/callback", state), CancellationToken.None));

		storage.Verify(x => x.AddToken(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(),
			It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	private sealed class TestHttpClientFactory(HttpMessageHandler handler) : Google.Apis.Http.HttpClientFactory
	{
		protected override HttpMessageHandler CreateHandler(CreateHttpClientArgs args) => handler;
	}

	private sealed class OAuthHandler : HttpMessageHandler
	{
		public int Requests { get; private set; }
		public string TokenRequest { get; private set; } = "";
		public string ChannelQuery { get; private set; } = "";
		public bool EmptyChannel { get; set; }
		public bool RejectToken { get; set; }
		public bool RejectChannel { get; set; }

		protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
		{
			Requests++;
			if (request.Method == HttpMethod.Post)
			{
				TokenRequest = await request.Content!.ReadAsStringAsync(ct);
				return RejectToken
					? new(HttpStatusCode.BadRequest) { Content = JsonContent.Create(new { error = "invalid_grant" }) }
					: new(HttpStatusCode.OK) { Content = JsonContent.Create(new { access_token = "callback-access", refresh_token = "callback-refresh", token_type = "Bearer", expires_in = 3600 }) };
			}
			ChannelQuery = request.RequestUri!.Query;
			return RejectChannel
				? new(HttpStatusCode.BadRequest) { Content = JsonContent.Create(new { error = new { code = 400, message = "lookup failed" } }) }
				: new(HttpStatusCode.OK)
				{
					Content = JsonContent.Create(new { items = EmptyChannel ? [] : new[] { new { id = "channel-id", snippet = new { title = "My Channel" } } } })
				};
		}
	}
}
