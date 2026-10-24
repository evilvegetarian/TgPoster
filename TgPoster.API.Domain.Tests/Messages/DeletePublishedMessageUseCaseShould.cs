using Moq;
using Security.Cryptography;
using Security.IdentityServices;
using Shouldly;
using TgPoster.API.Domain.ConfigModels;
using TgPoster.API.Domain.Services;
using TgPoster.API.Domain.UseCases.Messages.CreateMessagesFromFiles;
using TgPoster.API.Domain.UseCases.Messages.DeletePublishedMessage;
using TgPoster.Exceptions.BadRequest;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.Tests.Messages;

public sealed class DeletePublishedMessageUseCaseShould
{
	private const string Token = "123456:bot-token";
	private const long ChannelId = -1001234567890;
	private static readonly DateTimeOffset Now = new(2026, 10, 24, 12, 0, 0, TimeSpan.Zero);

	private readonly Guid messageId = Guid.NewGuid();
	private readonly Mock<IPublishedMessageService> publishedMessageService = new();
	private readonly Guid scheduleId = Guid.NewGuid();
	private readonly Mock<IDeletePublishedMessageStorage> storage = new();
	private readonly DeletePublishedMessageUseCase sut;
	private readonly Guid userId = Guid.NewGuid();

	public DeletePublishedMessageUseCaseShould()
	{
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

		publishedMessageService
			.Setup(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<int[]>(),
				It.IsAny<CancellationToken>()))
			.ReturnsAsync(ApiResult.Success());

		sut = new DeletePublishedMessageUseCase(storage.Object, identity.Object, tokenService,
			publishedMessageService.Object, new FixedTimeProvider(Now));
	}

	[Fact]
	public async Task ThrowMessageNotFound_WhenMessageNotOwnedOrMissing()
	{
		storage.Setup(x => x.GetMessageAsync(messageId, userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync((PublishedMessageInfo?)null);

		await Should.ThrowAsync<MessageNotFoundException>(async () =>
			await sut.Handle(new DeletePublishedMessageCommand(messageId), CancellationToken.None));

		VerifyNothingDeleted();
	}

	[Fact]
	public async Task ThrowMessageNotPublished_WhenMessageNotSent()
	{
		SetupMessage(Published() with { IsPublished = false });

		await Should.ThrowAsync<MessageNotPublishedException>(async () =>
			await sut.Handle(new DeletePublishedMessageCommand(messageId), CancellationToken.None));

		VerifyNothingDeleted();
	}

	[Fact]
	public async Task ThrowPublishedMessageIdsUnknown_WhenPostPublishedBeforeIdsWereSaved()
	{
		SetupMessage(Published() with { TelegramMessageIds = null });

		await Should.ThrowAsync<PublishedMessageIdsUnknownException>(async () =>
			await sut.Handle(new DeletePublishedMessageCommand(messageId), CancellationToken.None));

		VerifyNothingDeleted();
	}

	[Fact]
	public async Task ThrowPublishedMessageDeleteExpired_WhenDeleteWindowPassed()
	{
		SetupMessage(Published() with { TimePosting = Now.AddHours(-48).AddMinutes(5) });

		await Should.ThrowAsync<PublishedMessageDeleteExpiredException>(async () =>
			await sut.Handle(new DeletePublishedMessageCommand(messageId), CancellationToken.None));

		VerifyNothingDeleted();
	}

	[Fact]
	public async Task DeleteAllMessagesFromChannelAndList_WhenWithinDeleteWindow()
	{
		SetupMessage(Published() with { TimePosting = Now.AddHours(-48).AddMinutes(6) });

		await sut.Handle(new DeletePublishedMessageCommand(messageId), CancellationToken.None);

		publishedMessageService.Verify(x => x.DeleteAsync(Token, ChannelId,
			It.Is<int[]>(ids => ids.SequenceEqual(new[] { 10, 11, 12 })), It.IsAny<CancellationToken>()), Times.Once);
		storage.Verify(x => x.DeleteMessageAsync(messageId, It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task ThrowPublishedMessageDeleteFailed_WhenTelegramRejectsDelete()
	{
		SetupMessage(Published());
		publishedMessageService
			.Setup(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<int[]>(),
				It.IsAny<CancellationToken>()))
			.ReturnsAsync(ApiResult.Failure("Bad Request: message can't be deleted"));

		var exception = await Should.ThrowAsync<PublishedMessageDeleteFailedException>(async () =>
			await sut.Handle(new DeletePublishedMessageCommand(messageId), CancellationToken.None));

		exception.Message.ShouldContain("message can't be deleted");
		storage.Verify(x => x.DeleteMessageAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	private PublishedMessageInfo Published() =>
		new()
		{
			ScheduleId = scheduleId,
			ChannelId = ChannelId,
			TimePosting = Now.AddHours(-1),
			IsPublished = true,
			TelegramMessageIds = [10, 11, 12]
		};

	private void SetupMessage(PublishedMessageInfo message)
	{
		storage.Setup(x => x.GetMessageAsync(messageId, userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(message);
	}

	private void VerifyNothingDeleted()
	{
		publishedMessageService.Verify(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<int[]>(),
			It.IsAny<CancellationToken>()), Times.Never);
		storage.Verify(x => x.DeleteMessageAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
	{
		public override DateTimeOffset GetUtcNow() => now;
	}
}
