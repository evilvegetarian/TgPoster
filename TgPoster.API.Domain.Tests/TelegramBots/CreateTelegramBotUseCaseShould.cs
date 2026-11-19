using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using Moq;
using Security.Cryptography;
using Security.IdentityServices;
using Shared.Telegram;
using Shouldly;
using Telegram.Bot;
using TgPoster.API.Domain.ConfigModels;
using TgPoster.API.Domain.UseCases.TelegramBots.CreateTelegramBot;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.Tests.TelegramBots;

public sealed class CreateTelegramBotUseCaseShould : IDisposable
{
	private const string Token = "123456:ABC-DEF1234ghIkl-zyx57W2v1u123ew11";
	private readonly Guid userId = Guid.NewGuid();
	private readonly Mock<ICreateTelegramBotStorage> storage = new();
	private readonly Mock<ICryptoAES> crypto = new();
	private readonly TelegramBotManager manager = new();
	private readonly BotHandler handler = new();
	private readonly HttpClient http;
	private readonly CreateTelegramBotUseCase sut;

	public CreateTelegramBotUseCaseShould()
	{
		var identity = new Mock<IIdentityProvider>();
		identity.SetupGet(x => x.Current).Returns(new Identity(userId));
		http = new HttpClient(handler);
		var clients = (ConcurrentDictionary<string, TelegramBotClient>)typeof(TelegramBotManager)
			.GetField("clients", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(manager)!;
		clients[Token] = new(Token, http);
		crypto.Setup(x => x.Encrypt("test-key", Token)).Returns("encrypted-test-token");
		sut = new(storage.Object, identity.Object, crypto.Object, new TelegramOptions { SecretKey = "test-key" }, manager);
	}

	public void Dispose() { manager.Dispose(); http.Dispose(); }

	[Fact]
	public async Task PersistEncryptedTokenAndNotificationChat_ForCurrentOwner()
	{
		var id = Guid.NewGuid();
		storage.Setup(x => x.CreateTelegramBotAsync("encrypted-test-token", 100, userId, "test_bot", CancellationToken.None)).ReturnsAsync(id);

		var response = await sut.Handle(new(Token), CancellationToken.None);

		response.Id.ShouldBe(id);
		storage.VerifyAll();
		crypto.Verify(x => x.Encrypt("test-key", Token), Times.Once);
		handler.Calls.ShouldBe(["getupdates", "getme"]);
	}

	[Fact]
	public async Task RejectBotWithoutNotificationChat_BeforeEncryptingOrSavingTheToken()
	{
		handler.NoMessages = true;

		await Should.ThrowAsync<ChatIdNotFoundException>(() => sut.Handle(new(Token), CancellationToken.None));

		storage.VerifyNoOtherCalls();
		crypto.Verify(x => x.Encrypt(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
		handler.Calls.ShouldBe(["getupdates"]);
	}

	private sealed class BotHandler : HttpMessageHandler
	{
		public bool NoMessages { get; set; }
		public List<string> Calls { get; } = [];

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
		{
			var method = request.RequestUri!.Segments[^1].ToLowerInvariant();
			Calls.Add(method);
			object result = method switch
			{
				"getupdates" => NoMessages ? Array.Empty<object>() : new object[]
				{
					new { update_id = 1 },
					new { update_id = 2, message = new { message_id = 3, date = 1700000000, chat = new { id = 100, type = "private", first_name = "Test" } } }
				},
				"getme" => new { id = 123456, is_bot = true, first_name = "Test Bot", username = "test_bot" },
				_ => throw new InvalidOperationException("Unexpected test request: " + method)
			};
			return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { ok = true, result }) });
		}
	}
}
