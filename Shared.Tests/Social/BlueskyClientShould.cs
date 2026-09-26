using System.Net;
using System.Text.Json;
using Moq;
using Moq.Protected;
using Shared.Social.Bluesky;
using Shouldly;

namespace Shared.Tests.Social;

/// <summary>
///     Тесты для BlueskyClient
/// </summary>
public sealed class BlueskyClientShould
{
	private readonly Mock<IHttpClientFactory> httpClientFactoryMock;
	private readonly Mock<HttpMessageHandler> httpMessageHandlerMock;
	private HttpRequestMessage? lastRequest;

	public BlueskyClientShould()
	{
		httpMessageHandlerMock = new Mock<HttpMessageHandler>();
		httpClientFactoryMock = new Mock<IHttpClientFactory>();

		var httpClient = new HttpClient(httpMessageHandlerMock.Object);
		httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
	}

	[Fact]
	public async Task CreateSession_ParsesDidDocAndReturnsPdsUrl()
	{
		var json = """
		           {
		           	"did": "did:plc:abc",
		           	"handle": "name.bsky.social",
		           	"accessJwt": "access",
		           	"refreshJwt": "refresh",
		           	"didDoc": {
		           		"service": [
		           			{
		           				"id": "#atproto_pds",
		           				"type": "AtprotoPersonalDataServer",
		           				"serviceEndpoint": "https://morel.us-east.host.bsky.network/"
		           			}
		           		]
		           	}
		           }
		           """;
		SetupResponse(HttpStatusCode.OK, json);

		var client = new BlueskyClient(httpClientFactoryMock.Object);
		var result = await client.CreateSessionAsync("user.bsky.social", "pass", CancellationToken.None);

		result.IsSuccess.ShouldBeTrue();
		result.Value.ShouldNotBeNull();
		result.Value!.Did.ShouldBe("did:plc:abc");
		result.Value.Handle.ShouldBe("name.bsky.social");
		result.Value.AccessJwt.ShouldBe("access");
		result.Value.RefreshJwt.ShouldBe("refresh");
		result.Value.PdsUrl.ShouldBe("https://morel.us-east.host.bsky.network");

		lastRequest.ShouldNotBeNull();
		lastRequest!.RequestUri!.AbsoluteUri.ShouldBe("https://bsky.social/xrpc/com.atproto.server.createSession");
		lastRequest.Method.ShouldBe(HttpMethod.Post);
		var body = await lastRequest.Content!.ReadAsStringAsync();
		body.ShouldContain("\"identifier\":\"user.bsky.social\"");
		body.ShouldContain("\"password\":\"pass\"");
	}

	[Fact]
	public async Task CreateSession_WithoutDidDoc_FallsBackToEntrywayUrl()
	{
		var json = """{"did":"did:plc:x","handle":"x.bsky.social","accessJwt":"a","refreshJwt":"r"}""";
		SetupResponse(HttpStatusCode.OK, json);

		var client = new BlueskyClient(httpClientFactoryMock.Object);
		var result = await client.CreateSessionAsync("x", "p", CancellationToken.None);

		result.Value!.PdsUrl.ShouldBe("https://bsky.social");
	}

	[Fact]
	public async Task CreateSession_Unauthorized_ReturnsAuthError()
	{
		var json = """{"error":"AuthenticationRequired","message":"Invalid identifier or password"}""";
		SetupResponse(HttpStatusCode.Unauthorized, json);

		var result = await new BlueskyClient(httpClientFactoryMock.Object).CreateSessionAsync("x", "p", CancellationToken.None);

		result.IsSuccess.ShouldBeFalse();
		result.ErrorKind.ShouldBe(BlueskyErrorKind.Auth);
		result.Error.ShouldNotBeNull();
		result.Error.ShouldContain("AuthenticationRequired");
	}

	[Fact]
	public async Task CreateSession_ServerError_ReturnsTransientError()
	{
		SetupResponse(HttpStatusCode.InternalServerError, "");

		var result = await new BlueskyClient(httpClientFactoryMock.Object).CreateSessionAsync("x", "p", CancellationToken.None);

		result.ErrorKind.ShouldBe(BlueskyErrorKind.Transient);
		result.Error.ShouldBe("HTTP 500");
	}

	[Fact]
	public async Task CreateSession_NetworkFailure_ReturnsTransientError()
	{
		httpMessageHandlerMock
			.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>()
			)
			.ThrowsAsync(new HttpRequestException("No connection"));

		var result = await new BlueskyClient(httpClientFactoryMock.Object).CreateSessionAsync("x", "p", CancellationToken.None);

		result.ErrorKind.ShouldBe(BlueskyErrorKind.Transient);
		result.Error.ShouldNotBeNull();
		result.Error.ShouldContain("No connection");
	}

	[Fact]
	public async Task CreateSession_PropagatesCancellation()
	{
		var cts = new CancellationTokenSource();
		cts.Cancel();

		httpMessageHandlerMock
			.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>()
			)
			.ThrowsAsync(new OperationCanceledException());

		await Should.ThrowAsync<OperationCanceledException>(async () =>
			await new BlueskyClient(httpClientFactoryMock.Object).CreateSessionAsync("x", "p", cts.Token));
	}

	[Fact]
	public async Task UploadBlob_ParsesBlobAndSetsHeaders()
	{
		var json = """
		           {
		           	"blob": {
		           		"$type": "blob",
		           		"ref": { "$link": "bafkrei..." },
		           		"mimeType": "image/jpeg",
		           		"size": 12345
		           	}
		           }
		           """;
		SetupResponse(HttpStatusCode.OK, json);

		var session = new BlueskySession("did:plc:abc", "handle", "access", "refresh", "https://pds.example.com");
		var data = new byte[] { 1, 2, 3 };

		var result = await new BlueskyClient(httpClientFactoryMock.Object).UploadBlobAsync(session, data, "image/jpeg", CancellationToken.None);

		result.IsSuccess.ShouldBeTrue();
		result.Value!.Link.ShouldBe("bafkrei...");
		result.Value.MimeType.ShouldBe("image/jpeg");
		result.Value.Size.ShouldBe(12345);

		lastRequest!.RequestUri!.AbsoluteUri.ShouldBe("https://pds.example.com/xrpc/com.atproto.repo.uploadBlob");
		lastRequest.Headers.Authorization!.Scheme.ShouldBe("Bearer");
		lastRequest.Headers.Authorization.Parameter.ShouldBe("access");
		lastRequest.Content!.Headers.ContentType!.MediaType.ShouldBe("image/jpeg");
	}

	[Fact]
	public async Task CreatePost_WithImages_SerializesFacetsAndEmbed()
	{
		var json = """{"uri":"at://did:plc:abc/app.bsky.feed.post/3kxyz","cid":"bafyrei..."}""";
		SetupResponse(HttpStatusCode.OK, json);

		var session = new BlueskySession("did:plc:abc", "handle", "access", "refresh", "https://pds.example.com");
		var record = new BlueskyPostRecord
		{
			Text = "hello",
			CreatedAt = new DateTimeOffset(2026, 9, 26, 10, 0, 0, TimeSpan.Zero),
			Facets = [new BlueskyFacet(0, 5, "https://t.me/c/1")],
			Images = [new BlueskyImage(new BlueskyBlob("bafkrei...", "image/jpeg", 12345), "", 800, 600)]
		};

		var result = await new BlueskyClient(httpClientFactoryMock.Object).CreatePostAsync(session, record, CancellationToken.None);

		result.IsSuccess.ShouldBeTrue();
		result.Value!.Uri.ShouldBe("at://did:plc:abc/app.bsky.feed.post/3kxyz");
		result.Value.RecordKey.ShouldBe("3kxyz");

		var body = await lastRequest!.Content!.ReadAsStringAsync();
		using var doc = JsonDocument.Parse(body);
		var rec = doc.RootElement.GetProperty("record");

		rec.GetProperty("$type").GetString().ShouldBe("app.bsky.feed.post");
		rec.GetProperty("text").GetString().ShouldBe("hello");
		rec.GetProperty("createdAt").GetString().ShouldBe("2026-09-26T10:00:00.000Z");
		rec.GetProperty("langs")[0].GetString().ShouldBe("ru");

		var facet = rec.GetProperty("facets")[0];
		facet.GetProperty("index").GetProperty("byteStart").GetInt32().ShouldBe(0);
		facet.GetProperty("index").GetProperty("byteEnd").GetInt32().ShouldBe(5);
		facet.GetProperty("features")[0].GetProperty("$type").GetString().ShouldBe("app.bsky.richtext.facet#link");
		facet.GetProperty("features")[0].GetProperty("uri").GetString().ShouldBe("https://t.me/c/1");

		var embed = rec.GetProperty("embed");
		embed.GetProperty("$type").GetString().ShouldBe("app.bsky.embed.images");
		var image = embed.GetProperty("images")[0];
		image.GetProperty("alt").GetString().ShouldBe("");
		image.GetProperty("image").GetProperty("$type").GetString().ShouldBe("blob");
		image.GetProperty("image").GetProperty("ref").GetProperty("$link").GetString().ShouldBe("bafkrei...");
		image.GetProperty("aspectRatio").GetProperty("width").GetInt32().ShouldBe(800);
		image.GetProperty("aspectRatio").GetProperty("height").GetInt32().ShouldBe(600);
	}

	[Fact]
	public async Task CreatePost_WithExternal_SerializesExternalEmbed()
	{
		var json = """{"uri":"at://did/app.bsky.feed.post/1","cid":"cid"}""";
		SetupResponse(HttpStatusCode.OK, json);

		var session = new BlueskySession("did:plc:abc", "handle", "access", "refresh", "https://pds.example.com");
		var record = new BlueskyPostRecord
		{
			Text = "link",
			CreatedAt = DateTimeOffset.UtcNow,
			External = new BlueskyExternalEmbed("https://example.com", "Title", "Description")
		};

		var result = await new BlueskyClient(httpClientFactoryMock.Object).CreatePostAsync(session, record, CancellationToken.None);

		result.IsSuccess.ShouldBeTrue();

		var body = await lastRequest!.Content!.ReadAsStringAsync();
		using var doc = JsonDocument.Parse(body);
		var embed = doc.RootElement.GetProperty("record").GetProperty("embed");

		embed.GetProperty("$type").GetString().ShouldBe("app.bsky.embed.external");
		embed.GetProperty("external").GetProperty("uri").GetString().ShouldBe("https://example.com");
		embed.GetProperty("external").GetProperty("title").GetString().ShouldBe("Title");
		embed.GetProperty("external").GetProperty("description").GetString().ShouldBe("Description");
	}

	[Fact]
	public async Task CreatePost_WithReply_SerializesReply()
	{
		var json = """{"uri":"at://did/app.bsky.feed.post/1","cid":"cid"}""";
		SetupResponse(HttpStatusCode.OK, json);

		var session = new BlueskySession("did:plc:abc", "handle", "access", "refresh", "https://pds.example.com");
		var record = new BlueskyPostRecord
		{
			Text = "reply",
			CreatedAt = DateTimeOffset.UtcNow,
			Reply = new BlueskyReplyRef(
				new BlueskyRecordRef("at://root", "cid1"),
				new BlueskyRecordRef("at://parent", "cid2"))
		};

		await new BlueskyClient(httpClientFactoryMock.Object).CreatePostAsync(session, record, CancellationToken.None);

		var body = await lastRequest!.Content!.ReadAsStringAsync();
		using var doc = JsonDocument.Parse(body);
		var reply = doc.RootElement.GetProperty("record").GetProperty("reply");

		reply.GetProperty("root").GetProperty("uri").GetString().ShouldBe("at://root");
		reply.GetProperty("root").GetProperty("cid").GetString().ShouldBe("cid1");
		reply.GetProperty("parent").GetProperty("uri").GetString().ShouldBe("at://parent");
		reply.GetProperty("parent").GetProperty("cid").GetString().ShouldBe("cid2");
	}

	[Fact]
	public async Task CreatePost_WithoutFacets_OmitsFacetsProperty()
	{
		var json = """{"uri":"at://did/app.bsky.feed.post/1","cid":"cid"}""";
		SetupResponse(HttpStatusCode.OK, json);

		var session = new BlueskySession("did:plc:abc", "handle", "access", "refresh", "https://pds.example.com");
		var record = new BlueskyPostRecord { Text = "plain", CreatedAt = DateTimeOffset.UtcNow };

		await new BlueskyClient(httpClientFactoryMock.Object).CreatePostAsync(session, record, CancellationToken.None);

		var body = await lastRequest!.Content!.ReadAsStringAsync();
		using var doc = JsonDocument.Parse(body);

		doc.RootElement.GetProperty("record").TryGetProperty("facets", out _).ShouldBeFalse();
	}

	[Theory]
	[InlineData("ExpiredToken", HttpStatusCode.BadRequest, BlueskyErrorKind.Auth)]
	[InlineData("InvalidRequest", HttpStatusCode.BadRequest, BlueskyErrorKind.Permanent)]
	public async Task CreatePost_ReturnsMappedErrorKind(string error, HttpStatusCode code, BlueskyErrorKind expected)
	{
		var json = $"{{\"error\":\"{error}\",\"message\":\"msg\"}}";
		SetupResponse(code, json);

		var session = new BlueskySession("did:plc:abc", "handle", "access", "refresh", "https://pds.example.com");
		var record = new BlueskyPostRecord { Text = "x", CreatedAt = DateTimeOffset.UtcNow };

		var result = await new BlueskyClient(httpClientFactoryMock.Object).CreatePostAsync(session, record, CancellationToken.None);

		result.ErrorKind.ShouldBe(expected);
	}

	[Fact]
	public async Task CreatePost_RateLimit_ReturnsTransientError()
	{
		SetupResponse(HttpStatusCode.TooManyRequests, "");

		var session = new BlueskySession("did:plc:abc", "handle", "access", "refresh", "https://pds.example.com");
		var record = new BlueskyPostRecord { Text = "x", CreatedAt = DateTimeOffset.UtcNow };

		var result = await new BlueskyClient(httpClientFactoryMock.Object).CreatePostAsync(session, record, CancellationToken.None);

		result.ErrorKind.ShouldBe(BlueskyErrorKind.Transient);
		result.Error.ShouldBe("HTTP 429");
	}

	[Fact]
	public async Task CreatePost_WithImagesAndExternal_ThrowsArgumentException()
	{
		var session = new BlueskySession("did:plc:abc", "handle", "access", "refresh", "https://pds.example.com");
		var record = new BlueskyPostRecord
		{
			Text = "x",
			CreatedAt = DateTimeOffset.UtcNow,
			Images = [new BlueskyImage(new BlueskyBlob("l", "image/jpeg", 1), "a", 1, 1)],
			External = new BlueskyExternalEmbed("u", "t", "d")
		};

		await Should.ThrowAsync<ArgumentException>(async () =>
			await new BlueskyClient(httpClientFactoryMock.Object).CreatePostAsync(session, record, CancellationToken.None));
	}

	private void SetupResponse(HttpStatusCode statusCode, string body)
	{
		lastRequest = null;
		httpMessageHandlerMock
			.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>()
			)
			.Callback<HttpRequestMessage, CancellationToken>((req, _) => lastRequest = req)
			.ReturnsAsync(new HttpResponseMessage
			{
				StatusCode = statusCode,
				Content = new StringContent(body)
			});
	}
}
