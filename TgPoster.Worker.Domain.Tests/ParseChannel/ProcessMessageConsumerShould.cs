using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Security.Cryptography;
using Shared.Services;
using Shared.Telegram;
using Shared.Video;
using TgPoster.Telegram.Abstractions;
using TgPoster.Worker.Domain.ConfigModels;
using TgPoster.Worker.Domain.UseCases;
using TgPoster.Worker.Domain.UseCases.ParseChannel;
using TgPoster.Worker.Domain.UseCases.ProcessMessageConsumer;

namespace TgPoster.Worker.Domain.Tests.ParseChannel;

public sealed class ProcessMessageConsumerShould
{
	[Fact]
	public async Task SkipTelegramAndCreation_WhenSameProcessedCommandIsDeliveredAgain()
	{
		var sourceId = Guid.NewGuid();
		var storage = new Mock<IProcessMessageConsumerStorage>();
		storage.Setup(x => x.IsSourceMessageParsedAsync(sourceId, 10, It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);
		var messages = new Mock<ITelegramMessageService>(MockBehavior.Strict);
		using var botManager = new TelegramBotManager();
		var sut = new ProcessMessageConsumer(new VideoService(), new Mock<ICryptoAES>().Object,
			new TelegramOptions { SecretKey = "test-key", TelegramSessionId = Guid.Empty },
			new TelegramExecuteServices(NullLogger<TelegramExecuteServices>.Instance),
			new TimePostingService(), storage.Object, messages.Object, botManager,
			NullLogger<ProcessMessageConsumer>.Instance);
		var context = new Mock<ConsumeContext<ProcessMessage>>();
		context.SetupGet(x => x.Message).Returns(new ProcessMessage
		{
			Id = sourceId, TelegramBotId = Guid.NewGuid(),
			Messages = [new MessageCommand { Id = 11 }, new MessageCommand { Id = 10 }]
		});

		await sut.Consume(context.Object);
		await sut.Consume(context.Object);

		storage.Verify(x => x.IsSourceMessageParsedAsync(sourceId, 10, It.IsAny<CancellationToken>()), Times.Exactly(2));
		storage.Verify(x => x.GetChannelParsingParametersAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
		storage.Verify(x => x.CreateMessageAsync(It.IsAny<MessageDto>(), It.IsAny<CancellationToken>()), Times.Never);
		messages.VerifyNoOtherCalls();
	}
}
