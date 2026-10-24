using Moq;
using Security.Cryptography;
using Security.IdentityServices;
using Shared.OpenRouter;
using Shared.Telegram;
using Shouldly;
using TgPoster.API.Domain.ConfigModels;
using TgPoster.API.Domain.Services;
using TgPoster.API.Domain.UseCases.Messages.GenerateAiContent;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.Tests.Messages;

public sealed class GenerateAiContentHandlerShould : IDisposable
{
	private readonly TelegramBotManager botManager = new();
	private readonly Mock<IGenerateAiContentStorage> storage = new();
	private readonly GenerateAiContentHandler sut;
	private readonly Guid userId = Guid.NewGuid();

	public GenerateAiContentHandlerShould()
	{
		var identity = new Mock<IIdentityProvider>();
		identity.Setup(x => x.Current).Returns(new Identity(userId));
		var aes = new Mock<ICryptoAES>();
		var tokenService = new TelegramTokenService(new Mock<IGetTelegramBotStorage>().Object, identity.Object,
			aes.Object, new TelegramOptions { SecretKey = "key" });

		sut = new GenerateAiContentHandler(storage.Object, new Mock<IOpenRouterClient>().Object, aes.Object,
			new Mock<ITelegramService>().Object, tokenService, botManager,
			new OpenRouterOptions { SecretKey = "key" }, identity.Object);
	}

	public void Dispose()
	{
		botManager.Dispose();
	}

	[Fact]
	public async Task ThrowNotFound_WhenMessageBelongsToAnotherUser()
	{
		var messageId = Guid.NewGuid();
		storage.Setup(x => x.GetMessageAsync(messageId, userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync((GenerateAiContentMessageDto?)null);

		await Should.ThrowAsync<MessageNotFoundException>(() =>
			sut.Handle(new GenerateAiContentCommand(messageId), CancellationToken.None));

		storage.Verify(x => x.GetOpenRouterAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
			Times.Never);
	}
}
