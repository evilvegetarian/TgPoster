using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using Google;
using Google.Apis.Http;
using Moq;
using Security.Cryptography;
using Security.IdentityServices;
using Shared.Telegram;
using Shared.YouTube;
using Shouldly;
using Telegram.Bot;
using TgPoster.API.Domain.ConfigModels;
using TgPoster.API.Domain.Services;
using TgPoster.API.Domain.UseCases.Messages.CreateMessagesFromFiles;
using TgPoster.API.Domain.UseCases.Messages.ListMessage;
using TgPoster.API.Domain.UseCases.YouTubeAccount.SendVideoOnYouTube;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.Tests.YouTubeAccount;

public sealed class SendVideoOnYouTubeUseCaseShould : IDisposable
{
	private const string Token = "123456:ABC-DEF1234ghIkl-zyx57W2v1u123ew11";
	private readonly Guid messageId = Guid.NewGuid();
	private readonly Guid userId = Guid.NewGuid();
	private readonly Mock<ISendVideoOnYouTubeStorage> storage = new();
	private readonly TelegramBotManager manager = new();
	private readonly UploadHandler handler = new();
	private readonly HttpClient http;
	private readonly SendVideoOnYouTubeUseCase sut;
	private readonly YouTubeAccountDto account = new()
	{
		Id = Guid.NewGuid(), AccessToken = "test-access", RefreshToken = "test-refresh",
		ClientId = "test-client", ClientSecret = "test-secret"
	};

	public SendVideoOnYouTubeUseCaseShould()
	{
		var identity = new Mock<IIdentityProvider>();
		identity.SetupGet(x => x.Current).Returns(new Identity(userId));
		var bots = new Mock<IGetTelegramBotStorage>();
		bots.Setup(x => x.GetTelegramBotByMessageIdAsync(messageId, userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new TelegramBotDto { ApiTelegram = "encrypted", ChatId = 100 });
		var crypto = new Mock<ICryptoAES>();
		crypto.Setup(x => x.Decrypt("test-key", "encrypted")).Returns(Token);
		http = new HttpClient(handler);
		var clients = (ConcurrentDictionary<string, TelegramBotClient>)typeof(TelegramBotManager)
			.GetField("clients", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(manager)!;
		clients[Token] = new(Token, http);
		var tokenService = new TelegramTokenService(bots.Object, identity.Object, crypto.Object, new TelegramOptions { SecretKey = "test-key" });
		sut = new(storage.Object, identity.Object, tokenService, new YouTubeService(new TestHttpClientFactory(handler)), manager);
		storage.Setup(x => x.GetVideoFileMessageAsync(messageId, userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync([Video("first"), Video("second")]);
		storage.Setup(x => x.GetAccessTokenAsync(messageId, userId, It.IsAny<CancellationToken>())).ReturnsAsync(account);
	}

	public void Dispose() { manager.Dispose(); http.Dispose(); }

	[Fact]
	public async Task RejectMissingOrForeignMessage_BeforeReadingTheAccount()
	{
		storage.Setup(x => x.GetVideoFileMessageAsync(messageId, userId, It.IsAny<CancellationToken>())).ReturnsAsync([]);

		await Should.ThrowAsync<MessageNotFoundException>(() => sut.Handle(new(messageId), CancellationToken.None));

		storage.Verify(x => x.GetAccessTokenAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
		handler.Downloads.ShouldBe(0);
	}

	[Fact]
	public async Task RejectMessageWithoutYouTubeAccount_BeforeDownloadingVideos()
	{
		storage.Setup(x => x.GetAccessTokenAsync(messageId, userId, It.IsAny<CancellationToken>())).ReturnsAsync((YouTubeAccountDto?)null);

		await Should.ThrowAsync<Exception>(() => sut.Handle(new(messageId), CancellationToken.None));

		handler.Downloads.ShouldBe(0);
		handler.Uploads.ShouldBe(0);
	}

	[Fact]
	public async Task UploadEveryOwnedVideo_AndPersistRefreshedTokensAfterEachSuccessfulUpload()
	{
		await sut.Handle(new(messageId), CancellationToken.None);

		handler.Downloads.ShouldBe(2);
		handler.Uploads.ShouldBe(2);
		storage.Verify(x => x.GetVideoFileMessageAsync(messageId, userId, CancellationToken.None), Times.Once);
		storage.Verify(x => x.UpdateYouTubeTokensAsync(account.Id, "updated-access", "test-refresh", CancellationToken.None), Times.Exactly(2));
	}

	[Fact]
	public async Task NotPersistTokens_WhenUploadFails()
	{
		handler.RejectUpload = true;

		await Should.ThrowAsync<GoogleApiException>(() => sut.Handle(new(messageId), CancellationToken.None));

		handler.Downloads.ShouldBe(1);
		storage.Verify(x => x.UpdateYouTubeTokensAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(),
			It.IsAny<CancellationToken>()), Times.Never);
	}

	private static FileDto Video(string fileId) => new() { Id = Guid.NewGuid(), TgFileId = fileId, ContentType = "video/mp4" };

	private sealed class TestHttpClientFactory(HttpMessageHandler handler) : Google.Apis.Http.HttpClientFactory
	{
		protected override HttpMessageHandler CreateHandler(CreateHttpClientArgs args) => handler;
	}

	private sealed class UploadHandler : HttpMessageHandler
	{
		public int Downloads { get; private set; }
		public int Uploads { get; private set; }
		public bool RejectUpload { get; set; }

		protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
		{
			var uri = request.RequestUri!;
			if (uri.AbsolutePath.Contains("/file/", StringComparison.Ordinal))
			{
				Downloads++;
				return new(HttpStatusCode.OK) { Content = new ByteArrayContent([1, 2, 3]) };
			}
			if (uri.AbsolutePath.EndsWith("/getFile", StringComparison.OrdinalIgnoreCase))
				return new(HttpStatusCode.OK) { Content = JsonContent.Create(new { ok = true, result = new { file_id = "video", file_unique_id = "unique-video", file_path = "videos/test.mp4" } }) };
			if (uri.AbsolutePath.EndsWith("/token", StringComparison.Ordinal))
				return new(HttpStatusCode.OK) { Content = JsonContent.Create(new { access_token = "updated-access", expires_in = 3600, token_type = "Bearer" }) };
			if (request.Method == HttpMethod.Post)
			{
				var response = new HttpResponseMessage(HttpStatusCode.OK);
				response.Headers.Location = new Uri("https://upload.example.test/video");
				return response;
			}
			(await request.Content!.ReadAsByteArrayAsync(ct)).ShouldBe(new byte[] { 1, 2, 3 });
			Uploads++;
			return RejectUpload
				? new(HttpStatusCode.BadRequest) { Content = JsonContent.Create(new { error = new { code = 400, message = "rejected" } }) }
				: new(HttpStatusCode.OK) { Content = JsonContent.Create(new { id = "uploaded-video" }) };
		}
	}
}
