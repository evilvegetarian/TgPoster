using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using TgPoster.Telegram.Configuration;
using TgPoster.Telegram.Internal;
using TgPoster.Telegram.Models;

namespace Shared.Tests;

public sealed class TelegramPublicLookupServiceShould
{
	[Theory]
	[InlineData("durov")]
	[InlineData("@durov")]
	[InlineData("t.me/durov")]
	[InlineData("https://t.me/durov")]
	[InlineData("HTTPS://T.ME/DUROV")]
	public async Task LookupAsync_AcceptsVariousInputFormats(string input)
	{
		const string html = """
		                    <div class="tgme_page_title"><span>Pavel Durov</span></div>
		                    """;
		var (service, handler) = CreateService(_ => CreateResponse(HttpStatusCode.OK, html));

		var result = await service.LookupAsync(input, CancellationToken.None);

		result.IsSuccess.ShouldBeTrue();
		result.Value!.Type.ShouldBe(TelegramEntityType.User);
		handler.LastRequestedUrl.ShouldNotBeNull();
		handler.LastRequestedUrl!.AbsoluteUri
			.ShouldBeOneOf("https://t.me/durov", "https://t.me/DUROV");
	}

	[Fact]
	public async Task LookupAsync_InvalidInput_ReturnsFailedUsernameNotFound()
	{
		var (service, _) = CreateService(_ => CreateResponse(HttpStatusCode.OK, ""));

		var result = await service.LookupAsync("not a username!", CancellationToken.None);

		result.IsSuccess.ShouldBeFalse();
		result.Status.ShouldBe(TelegramOperationStatus.UsernameNotFound);
	}

	[Fact]
	public async Task LookupAsync_ChannelPage_ReturnsChannelInfo()
	{
		const string html = """
		                    <div class="tgme_page_title"><span>News</span></div>
		                    <div class="tgme_page_description">All the news</div>
		                    <div class="tgme_page_extra">9 999 subscribers</div>
		                    """;
		var (service, _) = CreateService(_ => CreateResponse(HttpStatusCode.OK, html));

		var result = await service.LookupAsync("newschannel", CancellationToken.None);

		result.IsSuccess.ShouldBeTrue();
		result.Value!.Type.ShouldBe(TelegramEntityType.Channel);
		result.Value.Title.ShouldBe("News");
		result.Value.Description.ShouldBe("All the news");
		result.Value.MembersCount.ShouldBe(9999);
	}

	[Fact]
	public async Task LookupAsync_NotFound404_ReturnsSuccessWithNotFoundType()
	{
		var (service, _) = CreateService(_ => CreateResponse(HttpStatusCode.NotFound, ""));

		var result = await service.LookupAsync("ghost_username_xyz", CancellationToken.None);

		result.IsSuccess.ShouldBeTrue();
		result.Value!.Type.ShouldBe(TelegramEntityType.NotFound);
		result.Value.Username.ShouldBe("ghost_username_xyz");
	}

	[Fact]
	public async Task LookupAsync_ServerError500_ReturnsFailedUnknownError()
	{
		var (service, _) = CreateService(_ => CreateResponse(HttpStatusCode.InternalServerError, ""));

		var result = await service.LookupAsync("somechannel", CancellationToken.None);

		result.IsSuccess.ShouldBeFalse();
		result.Status.ShouldBe(TelegramOperationStatus.UnknownError);
	}

	[Fact]
	public async Task LookupAsync_HttpException_ReturnsFailedUnknownError()
	{
		var (service, _) = CreateService(_ => throw new HttpRequestException("connection refused"));

		var result = await service.LookupAsync("somechannel", CancellationToken.None);

		result.IsSuccess.ShouldBeFalse();
		result.Status.ShouldBe(TelegramOperationStatus.UnknownError);
		result.ErrorMessage!.ShouldContain("connection refused");
	}

	[Fact]
	public async Task LookupAsync_Timeout_ReturnsFailedTimeout()
	{
		var (service, _) = CreateService(_ => throw new TaskCanceledException("timeout"));

		var result = await service.LookupAsync("somechannel", CancellationToken.None);

		result.IsSuccess.ShouldBeFalse();
		result.Status.ShouldBe(TelegramOperationStatus.Timeout);
	}

	private static (TelegramPublicLookupService Service, RecordingHandler Handler) CreateService(
		Func<HttpRequestMessage, HttpResponseMessage> responder
	)
	{
		var handler = new RecordingHandler(responder);
		var httpClient = new HttpClient(handler);
		var factory = new SingleClientFactory(httpClient);
		var options = Options.Create(new TelegramPublicLookupOptions { MaxRetries = 0 });
		var service = new TelegramPublicLookupService(
			factory,
			options,
			NullLogger<TelegramPublicLookupService>.Instance);
		return (service, handler);
	}

	private static HttpResponseMessage CreateResponse(HttpStatusCode code, string body) =>
		new(code) { Content = new StringContent(body) };

	private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
	{
		public Uri? LastRequestedUrl { get; private set; }

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
		{
			LastRequestedUrl = request.RequestUri;
			return Task.FromResult(responder(request));
		}
	}

	private sealed class SingleClientFactory(HttpClient client) : IHttpClientFactory
	{
		public HttpClient CreateClient(string name) => client;
	}
}