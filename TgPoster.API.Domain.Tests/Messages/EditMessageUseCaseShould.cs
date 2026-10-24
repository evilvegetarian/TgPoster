using Microsoft.AspNetCore.Http;
using Moq;
using Security.Cryptography;
using Security.IdentityServices;
using Shared.Telegram;
using Shared.Utilities;
using Shouldly;
using Telegram.Bot;
using TgPoster.API.Domain.ConfigModels;
using TgPoster.API.Domain.Services;
using TgPoster.API.Domain.UseCases.Messages.CreateMessagesFromFiles;
using TgPoster.API.Domain.UseCases.Messages.EditMessage;
using TgPoster.Exceptions.BadRequest;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.Tests.Messages;

public sealed class EditMessageUseCaseShould
{
	private const string Token = "123456:bot-token";
	private const long ChannelId = -1001234567890;
	private static readonly DateTimeOffset Now = new(2026, 10, 24, 12, 0, 0, TimeSpan.Zero);
	private static readonly DateTimeOffset PublishedAt = Now.AddDays(-1);

	private readonly Guid messageId = Guid.NewGuid();
	private readonly Mock<IPublishedMessageService> publishedMessageService = new();
	private readonly Guid scheduleId = Guid.NewGuid();
	private readonly Mock<IEditMessageStorage> storage = new();
	private readonly EditMessageUseCase sut;
	private readonly Guid userId = Guid.NewGuid();

	public EditMessageUseCaseShould()
	{
		var identity = new Mock<IIdentityProvider>();
		identity.Setup(x => x.Current).Returns(new Identity(userId));

		var botStorage = new Mock<IGetTelegramBotStorage>();
		botStorage
			.Setup(x => x.GetTelegramBotByScheduleIdAsync(It.IsAny<Guid>(), userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new TelegramBotDto { ApiTelegram = "encrypted", ChatId = 1 });
		var aes = new Mock<ICryptoAES>();
		aes.Setup(x => x.Decrypt(It.IsAny<string>(), "encrypted")).Returns(Token);
		var tokenService = new TelegramTokenService(botStorage.Object, identity.Object, aes.Object,
			new TelegramOptions { SecretKey = "key" });

		var telegramService = new Mock<ITelegramService>();
		telegramService
			.Setup(x => x.GetFileMessageInTelegramByFile(It.IsAny<TelegramBotClient>(), It.IsAny<List<IFormFile>>(),
				It.IsAny<long>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);
		publishedMessageService
			.Setup(x => x.EditTextAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<bool>(),
				It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(ApiResult.Success());

		sut = new EditMessageUseCase(storage.Object, identity.Object, tokenService, telegramService.Object,
			new TelegramBotManager(), publishedMessageService.Object, new FixedTimeProvider(Now));
	}

	[Fact]
	public async Task ThrowMessageNotFound_WhenMessageNotOwnedOrMissing()
	{
		storage.Setup(x => x.GetMessageAsync(messageId, userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync((EditMessageInfo?)null);

		await Should.ThrowAsync<MessageNotFoundException>(async () =>
			await sut.Handle(Command("текст", []), CancellationToken.None));

		VerifyNotUpdated();
	}

	[Fact]
	public async Task ThrowTimePostingInPast_WhenNotPublishedPostMovedToPast()
	{
		SetupMessage(Published([]) with { IsPublished = false });

		await Should.ThrowAsync<TimePostingInPastException>(async () =>
			await sut.Handle(Command("текст", []) with { TimePosting = Now.AddMinutes(-1) }, CancellationToken.None));

		VerifyNotUpdated();
	}

	[Fact]
	public async Task UpdateWithoutTelegramEdit_WhenNotPublished()
	{
		SetupMessage(Published([]) with { IsPublished = false });
		var command = Command("новый текст", []) with { TimePosting = Now.AddHours(1) };

		await sut.Handle(command, CancellationToken.None);

		storage.Verify(x => x.UpdateMessageAsync(command, It.IsAny<List<MediaFileResult>>(),
			It.IsAny<CancellationToken>()), Times.Once);
		VerifyNotEditedInTelegram();
	}

	[Fact]
	public async Task ThrowPublishedMessageMediaChange_WhenScheduleChanged()
	{
		SetupMessage(Published([]));

		await Should.ThrowAsync<PublishedMessageMediaChangeException>(async () =>
			await sut.Handle(Command("текст", []) with { ScheduleId = Guid.NewGuid() }, CancellationToken.None));

		VerifyNotUpdated();
	}

	[Fact]
	public async Task ThrowPublishedMessageMediaChange_WhenNewFilesAdded()
	{
		SetupMessage(Published([]));

		await Should.ThrowAsync<PublishedMessageMediaChangeException>(async () =>
			await sut.Handle(Command("текст", []) with { NewFiles = [Mock.Of<IFormFile>()] }, CancellationToken.None));

		VerifyNotUpdated();
	}

	[Fact]
	public async Task ThrowPublishedMessageMediaChange_WhenFileRemoved()
	{
		var first = Guid.NewGuid();
		SetupMessage(Published([first, Guid.NewGuid()]));

		await Should.ThrowAsync<PublishedMessageMediaChangeException>(async () =>
			await sut.Handle(Command("текст", [first]), CancellationToken.None));

		VerifyNotUpdated();
	}

	[Fact]
	public async Task ThrowPublishedMessageMediaChange_WhenFilesReordered()
	{
		var first = Guid.NewGuid();
		var second = Guid.NewGuid();
		SetupMessage(Published([first, second]));

		await Should.ThrowAsync<PublishedMessageMediaChangeException>(async () =>
			await sut.Handle(Command("текст", [second, first]), CancellationToken.None));

		VerifyNotUpdated();
	}

	[Fact]
	public async Task UpdateOtherFieldsWithOriginalTime_WhenPublishedTextNotChanged()
	{
		SetupMessage(Published([]) with
		{
			Text = "строка\nвторая",
			TelegramMessageIds = null,
			TelegramTextMessageId = null
		});
		var command = Command("строка\r\nвторая", []) with { TimePosting = Now.AddDays(3), CrossPostEnabled = false };

		await sut.Handle(command, CancellationToken.None);

		storage.Verify(x => x.UpdateMessageAsync(
			It.Is<EditMessageCommand>(c => c.TimePosting == PublishedAt && c.CrossPostEnabled == false),
			It.Is<List<MediaFileResult>>(files => files.Count == 0),
			It.IsAny<CancellationToken>()), Times.Once);
		VerifyNotEditedInTelegram();
	}

	[Fact]
	public async Task ThrowPublishedMessageTextNotEditable_WhenTextChangedOnLegacyPost()
	{
		List<Guid> files = [Guid.NewGuid()];
		SetupMessage(Published(files) with { TelegramMessageIds = null, TelegramTextMessageId = null });

		await Should.ThrowAsync<PublishedMessageTextNotEditableException>(async () =>
			await sut.Handle(Command("новый текст", [..files]), CancellationToken.None));

		VerifyNotUpdated();
		VerifyNotEditedInTelegram();
	}

	[Fact]
	public async Task ThrowPublishedMessageTextTooLong_WhenCaptionWithSignatureExceedsLimit()
	{
		List<Guid> files = [Guid.NewGuid()];
		SetupMessage(Published(files) with { Signature = "Подпись канала" });
		var text = new string('а', TelegramLimits.CaptionLength - 5);

		var exception = await Should.ThrowAsync<PublishedMessageTextTooLongException>(async () =>
			await sut.Handle(Command(text, [..files]), CancellationToken.None));

		exception.Message.ShouldContain(TelegramLimits.CaptionLength.ToString());
		VerifyNotUpdated();
		VerifyNotEditedInTelegram();
	}

	[Fact]
	public async Task EditCaptionWithSignature_WhenTextHolderIsFirstAlbumMessage()
	{
		List<Guid> files = [Guid.NewGuid(), Guid.NewGuid()];
		SetupMessage(Published(files) with
		{
			TelegramMessageIds = [10, 11],
			TelegramTextMessageId = 10,
			Signature = "Подпись"
		});

		await sut.Handle(Command("новый текст", [..files]), CancellationToken.None);

		publishedMessageService.Verify(x => x.EditTextAsync(Token, ChannelId, 10, true, "новый текст\n\nПодпись",
			It.IsAny<CancellationToken>()), Times.Once);
		storage.Verify(x => x.UpdateMessageAsync(It.Is<EditMessageCommand>(c => c.Text == "новый текст"),
			It.IsAny<List<MediaFileResult>>(), It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task EditMessageText_WhenTextSentSeparatelyFromAlbum()
	{
		List<Guid> files = [Guid.NewGuid(), Guid.NewGuid()];
		SetupMessage(Published(files) with { TelegramMessageIds = [10, 11, 12], TelegramTextMessageId = 12 });
		var text = new string('а', TelegramLimits.CaptionLength + 100);

		await sut.Handle(Command(text, [..files]), CancellationToken.None);

		publishedMessageService.Verify(x => x.EditTextAsync(Token, ChannelId, 12, false, text,
			It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task EditMessageText_WhenPostHasNoMedia()
	{
		SetupMessage(Published([]));

		await sut.Handle(Command("новый текст", []), CancellationToken.None);

		publishedMessageService.Verify(x => x.EditTextAsync(Token, ChannelId, 42, false, "новый текст",
			It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task ThrowPublishedMessageEditFailed_WhenTelegramRejectsEdit()
	{
		SetupMessage(Published([]));
		publishedMessageService
			.Setup(x => x.EditTextAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<bool>(),
				It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(ApiResult.Failure("Bad Request: message to edit not found"));

		var exception = await Should.ThrowAsync<PublishedMessageEditFailedException>(async () =>
			await sut.Handle(Command("новый текст", []), CancellationToken.None));

		exception.Message.ShouldContain("message to edit not found");
		VerifyNotUpdated();
	}

	private EditMessageCommand Command(string? text, List<Guid> files) =>
		new(messageId, scheduleId, PublishedAt, text, files, []);

	private EditMessageInfo Published(List<Guid> fileIds) =>
		new()
		{
			ScheduleId = scheduleId,
			ChannelId = ChannelId,
			TimePosting = PublishedAt,
			IsPublished = true,
			Text = "старый текст",
			FileIds = fileIds,
			TelegramMessageIds = [42],
			TelegramTextMessageId = 42
		};

	private void SetupMessage(EditMessageInfo message)
	{
		storage.Setup(x => x.GetMessageAsync(messageId, userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(message);
	}

	private void VerifyNotUpdated()
	{
		storage.Verify(x => x.UpdateMessageAsync(It.IsAny<EditMessageCommand>(), It.IsAny<List<MediaFileResult>>(),
			It.IsAny<CancellationToken>()), Times.Never);
	}

	private void VerifyNotEditedInTelegram()
	{
		publishedMessageService.Verify(x => x.EditTextAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<int>(),
			It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
	{
		public override DateTimeOffset GetUtcNow() => now;
	}
}
