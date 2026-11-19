using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Google;
using Google.Apis.Http;
using Shared.YouTube;
using Shouldly;

namespace Shared.Tests;

public sealed class YouTubeServiceShould
{
	private static YouTubeAccountDto Account() => new()
	{
		AccessToken = "test-access", RefreshToken = "test-refresh", ClientId = "test-client", ClientSecret = "test-secret",
		DefaultTitle = "Default title", DefaultDescription = "Default description", DefaultTags = "default, shorts"
	};

	[Fact]
	public async Task RejectNullAccount_WithoutSendingRequests()
	{
		using var handler = new UploadHandler();
		var sut = new YouTubeService(new TestHttpClientFactory(handler));
		using var stream = new MemoryStream([1, 2, 3]);

		await Should.ThrowAsync<ArgumentNullException>(() => sut.UploadVideoAsync(null!, stream));

		handler.RequestCount.ShouldBe(0);
	}

	[Fact]
	public async Task RejectNullStream_WithoutSendingRequests()
	{
		using var handler = new UploadHandler();
		var sut = new YouTubeService(new TestHttpClientFactory(handler));

		await Should.ThrowAsync<ArgumentNullException>(() => sut.UploadVideoAsync(Account(), null!));

		handler.RequestCount.ShouldBe(0);
	}

	[Theory]
	[InlineData("tag1, tag2, ,", "tag1,tag2")]
	[InlineData("", "")]
	[InlineData(null, "default,shorts")]
	public async Task UploadVideoWithMetadata_AndReturnVideoIdAndRefreshedTokens(string? tags, string expectedTags)
	{
		using var handler = new UploadHandler();
		var sut = new YouTubeService(new TestHttpClientFactory(handler));
		using var stream = new MemoryStream([1, 2, 3]);

		var result = await sut.UploadVideoAsync(Account(), stream, "Title", "Description", tags);

		result.VideoId.ShouldBe("uploaded-video");
		result.AccessToken.ShouldBe("refreshed-access");
		result.RefreshToken.ShouldBe("test-refresh");
		handler.UploadedBytes.ShouldBe(new byte[] { 1, 2, 3 });
		var snippet = handler.Metadata.GetProperty("snippet");
		snippet.GetProperty("title").GetString().ShouldBe("Title");
		snippet.GetProperty("description").GetString().ShouldBe("Description");
		string.Join(',', snippet.GetProperty("tags").EnumerateArray().Select(x => x.GetString())).ShouldBe(expectedTags);
		handler.Metadata.GetProperty("status").GetProperty("privacyStatus").GetString().ShouldBe("public");
	}

	[Fact]
	public async Task UseAccountDefaults_WhenMetadataWasNotProvided()
	{
		using var handler = new UploadHandler();
		var sut = new YouTubeService(new TestHttpClientFactory(handler));
		using var stream = new MemoryStream([1]);

		await sut.UploadVideoAsync(Account(), stream);

		handler.Metadata.GetProperty("snippet").GetProperty("title").GetString().ShouldBe("Default title");
		handler.Metadata.GetProperty("snippet").GetProperty("description").GetString().ShouldBe("Default description");
	}

	[Fact]
	public async Task PropagateUploadFailure_WithoutReturningSuccess()
	{
		using var handler = new UploadHandler { RejectUpload = true };
		var sut = new YouTubeService(new TestHttpClientFactory(handler));
		using var stream = new MemoryStream([1]);

		var exception = await Should.ThrowAsync<GoogleApiException>(() => sut.UploadVideoAsync(Account(), stream));

		exception.HttpStatusCode.ShouldBe(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task RespectCancellation()
	{
		using var handler = new UploadHandler();
		var sut = new YouTubeService(new TestHttpClientFactory(handler));
		using var stream = new MemoryStream([1]);
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();

		await Should.ThrowAsync<OperationCanceledException>(() => sut.UploadVideoAsync(Account(), stream, ct: cancellation.Token));
	}

	private sealed class TestHttpClientFactory(HttpMessageHandler handler) : Google.Apis.Http.HttpClientFactory
	{
		protected override HttpMessageHandler CreateHandler(CreateHttpClientArgs args) => handler;
	}

	private sealed class UploadHandler : HttpMessageHandler
	{
		public int RequestCount { get; private set; }
		public JsonElement Metadata { get; private set; }
		public byte[] UploadedBytes { get; private set; } = [];
		public bool RejectUpload { get; init; }

		protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
		{
			ct.ThrowIfCancellationRequested();
			RequestCount++;
			if (request.RequestUri!.AbsolutePath.EndsWith("/token", StringComparison.Ordinal))
				return new(HttpStatusCode.OK) { Content = JsonContent.Create(new { access_token = "refreshed-access", expires_in = 3600, token_type = "Bearer" }) };
			if (request.Method == HttpMethod.Post)
			{
				await using var content = await request.Content!.ReadAsStreamAsync(ct);
				await using var decoded = request.Content.Headers.ContentEncoding.Contains("gzip")
					? new System.IO.Compression.GZipStream(content, System.IO.Compression.CompressionMode.Decompress)
					: (Stream)content;
				using var metadata = await JsonDocument.ParseAsync(decoded, cancellationToken: ct);
				Metadata = metadata.RootElement.Clone();
				var response = new HttpResponseMessage(HttpStatusCode.OK);
				response.Headers.Location = new Uri("https://upload.example.test/video");
				return response;
			}

			request.Method.ShouldBe(HttpMethod.Put);
			UploadedBytes = await request.Content!.ReadAsByteArrayAsync(ct);
			return RejectUpload
				? new(HttpStatusCode.BadRequest) { Content = JsonContent.Create(new { error = new { code = 400, message = "rejected" } }) }
				: new(HttpStatusCode.OK) { Content = JsonContent.Create(new { id = "uploaded-video" }) };
		}
	}
}
