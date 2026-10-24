using System.Collections.Concurrent;
using System.Net;
using System.Reflection;
using Moq;
using Moq.Protected;
using Security.Cryptography;
using Security.IdentityServices;
using Shared.Telegram;
using Telegram.Bot;
using TgPoster.API.Domain.ConfigModels;
using TgPoster.API.Domain.Services;
using TgPoster.API.Domain.UseCases.Messages.CreateMessagesFromFiles;
using TgPoster.API.Domain.UseCases.Parse.CreateParseChannel;
using TgPoster.Telegram.Abstractions;

namespace TgPoster.API.Domain.Tests.Parse;

public sealed class CreateParseChannelUseCaseShould : IDisposable
{
	private const string Token = "123456:bot-token";
	private const string ChannelName = "source_channel";
	private const int TotalMessagesCount = 100;

	private readonly TelegramBotManager botManager = new();
	private readonly Guid scheduleId = Guid.NewGuid();
	private readonly Guid sessionId = Guid.NewGuid();
	private readonly Mock<IParseChannelStorage> storage = new();
	private readonly CreateParseChannelUseCase sut;

	public CreateParseChannelUseCaseShould()
	{
		var userId = Guid.NewGuid();
		var identity = new Mock<IIdentityProvider>();
		identity.Setup(x => x.Current).Returns(new Identity(userId));

		var botStorage = new Mock<IGetTelegramBotStorage>();
		botStorage
			.Setup(x => x.GetTelegramBotByScheduleIdAsync(scheduleId, userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new TelegramBotDto { ApiTelegram = "encrypted", ChatId = 1 });
		var aes = new Mock<ICryptoAES>();
		aes.Setup(x => x.Decrypt(It.IsAny<string>(), "encrypted")).Returns(Token);
		var tokenService = new TelegramTokenService(botStorage.Object, identity.Object, aes.Object,
			new TelegramOptions { SecretKey = "key" });

		var chatService = new Mock<ITelegramChatService>();
		chatService.Setup(x => x.GetChannelMessagesCountAsync(sessionId, ChannelName))
			.ReturnsAsync(TotalMessagesCount);

		UseFakeBotApi("""
		              {"ok":true,"result":{"id":-1001234567890,"type":"channel","title":"Источник",
		              "username":"source_channel","accent_color_id":0,"max_reaction_count":11}}
		              """);

		sut = new CreateParseChannelUseCase(storage.Object, tokenService, chatService.Object, botManager);
	}

	public void Dispose()
	{
		botManager.Dispose();
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task PassSkipAdsToStorage_WhenCreating(bool skipAds)
	{
		var command = new CreateParseChannelCommand(ChannelName, true, scheduleId, false, false, [], false, null, null,
			false, sessionId, skipAds);

		await sut.Handle(command, CancellationToken.None);

		storage.Verify(x => x.AddParseChannelParametersAsync(ChannelName, true, scheduleId, false, false,
			It.IsAny<string[]>(), false, null, null, false, sessionId, TotalMessagesCount, skipAds,
			It.IsAny<CancellationToken>()), Times.Once);
	}

	/// <summary>
	/// Подменяет ответы Bot API для токена теста. Use case сам берёт клиента у TelegramBotManager, а тот создаёт
	/// TelegramBotClient с сетевым HttpClient, поэтому клиент с фейковым обработчиком кладётся прямо в его кэш
	/// </summary>
	/// <param name="responseJson"></param>
	private void UseFakeBotApi(string responseJson)
	{
		var handler = new Mock<HttpMessageHandler>();
		handler.Protected()
			.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(() => new HttpResponseMessage
			{
				StatusCode = HttpStatusCode.OK,
				Content = new StringContent(responseJson)
			});

		var clientsField = typeof(TelegramBotManager).GetField("clients", BindingFlags.NonPublic | BindingFlags.Instance)
		                   ?? throw new InvalidOperationException("В TelegramBotManager нет кэша clients");
		var clients = (ConcurrentDictionary<string, TelegramBotClient>)clientsField.GetValue(botManager)!;
		clients[Token] = new TelegramBotClient(Token, new HttpClient(handler.Object));
	}
}
