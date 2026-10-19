using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using TgPoster.Telegram.Abstractions;
using TgPoster.Telegram.Models;
using TgPoster.Worker.Domain.UseCases.SendCommentConsumer;

namespace TgPoster.Worker.Domain.Tests.SendComment;

public sealed class SendCommentConsumerShould
{
	private const long DiscussionGroupId = 777;
	private const long SourceChannelId = 555;
	private const int OriginalPostId = 10;

	private readonly List<string?> loggedErrors = [];
	private readonly Guid sessionId = Guid.NewGuid();
	private readonly Guid settingsId = Guid.NewGuid();
	private readonly Mock<ISendCommentConsumerStorage> storage = new();
	private readonly SendCommentConsumer sut;
	private readonly Mock<ITelegramMessageService> tgMessages = new();

	public SendCommentConsumerShould()
	{
		storage.Setup(s => s.CreateLogAsync(
				settingsId, OriginalPostId, It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string?>(),
				It.IsAny<CancellationToken>()))
			.Callback<Guid, int, int?, int?, string?, CancellationToken>((_, _, _, _, error, _) =>
				loggedErrors.Add(error))
			.Returns(Task.CompletedTask);

		var sourcePeer = TelegramPeer.Channel(SourceChannelId, 1);
		tgMessages.Setup(x => x.GetAllDialogsAsync(sessionId, It.IsAny<CancellationToken>(), It.IsAny<bool>()))
			.ReturnsAsync(TelegramOperationResult<IReadOnlyList<TelegramChatInfo>>.Success(
			[
				new TelegramChatInfo { Id = SourceChannelId, AccessHash = 1, Title = "source", Peer = sourcePeer }
			]));
		tgMessages.Setup(x => x.GetHistoryAsync(
				sessionId, sourcePeer, 1, It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<int>(),
				It.IsAny<CancellationToken>(), It.IsAny<bool>()))
			.ReturnsAsync(TelegramOperationResult<TelegramHistoryPage>.Success(new TelegramHistoryPage
			{
				Messages = [new TelegramMessage { Id = 42, Date = DateTime.UtcNow }]
			}));
		tgMessages.Setup(x => x.GetDiscussionMessageIdAsync(
				sessionId, It.IsAny<TelegramPeer>(), OriginalPostId, It.IsAny<CancellationToken>(), It.IsAny<bool>()))
			.ReturnsAsync(TelegramOperationResult<int?>.Success(100));
		tgMessages.Setup(x => x.ExportMessageLinkAsync(
				sessionId, sourcePeer, 42, It.IsAny<CancellationToken>(), It.IsAny<bool>()))
			.ReturnsAsync(TelegramOperationResult<string>.Success("https://t.me/source/42"));
		SetupSend(TelegramOperationResult<int?>.Success(500));

		sut = new SendCommentConsumer(storage.Object, tgMessages.Object, NullLogger<SendCommentConsumer>.Instance);
	}

	[Fact]
	public async Task SendCommentAndWriteSuccessLog_WhenAccountIsNotBanned()
	{
		await ConsumeAsync();

		loggedErrors.ShouldHaveSingleItem().ShouldBeNull();
		VerifyAccountBanNotRecorded();
	}

	[Fact]
	public async Task WriteFailureWithoutTelegram_WhenAccountBanIsAlreadyRecorded()
	{
		storage.Setup(s => s.IsAccountBannedAsync(sessionId, DiscussionGroupId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);

		await ConsumeAsync();

		loggedErrors.ShouldHaveSingleItem()!.ShouldContain("заблокирован");
		tgMessages.VerifyNoOtherCalls();
	}

	[Fact]
	public async Task RecordAccountBan_WhenSendFailsWithBan()
	{
		SetupSend(TelegramOperationResult<int?>.Failed(TelegramOperationStatus.ChannelBanned, "USER_BANNED_IN_CHANNEL"));

		await ConsumeAsync();

		storage.Verify(s => s.MarkAccountBannedAsync(
			sessionId, DiscussionGroupId, "USER_BANNED_IN_CHANNEL", It.IsAny<CancellationToken>()), Times.Once);
		loggedErrors.ShouldHaveSingleItem().ShouldBe("USER_BANNED_IN_CHANNEL");
	}

	[Fact]
	public async Task RecordRestrictionInDiscoverNotAsAccountBan_WhenChatForbidsWriting()
	{
		SetupSend(TelegramOperationResult<int?>.Failed(
			TelegramOperationStatus.NoWritePermission, "CHAT_WRITE_FORBIDDEN"));
		SetupMembership(isAccountRestricted: false);

		await ConsumeAsync();

		VerifyAccountBanNotRecorded();
		storage.Verify(s => s.MarkChatWriteForbiddenAsync(
			settingsId, DiscussionGroupId, It.IsAny<CancellationToken>()), Times.Once);
		loggedErrors.ShouldHaveSingleItem().ShouldBe("CHAT_WRITE_FORBIDDEN");
	}

	[Fact]
	public async Task RecordAccountBanNotDiscover_WhenWriteForbiddenBecauseAccountIsMuted()
	{
		SetupSend(TelegramOperationResult<int?>.Failed(
			TelegramOperationStatus.NoWritePermission, "CHAT_WRITE_FORBIDDEN"));
		SetupMembership(isAccountRestricted: true);

		await ConsumeAsync();

		storage.Verify(s => s.MarkAccountBannedAsync(
			sessionId, DiscussionGroupId, "CHAT_WRITE_FORBIDDEN", It.IsAny<CancellationToken>()), Times.Once);
		storage.Verify(s => s.MarkChatWriteForbiddenAsync(
			It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task RecordNothing_WhenWriteForbiddenAndRightsCannotBeChecked()
	{
		SetupSend(TelegramOperationResult<int?>.Failed(
			TelegramOperationStatus.NoWritePermission, "CHAT_WRITE_FORBIDDEN"));
		tgMessages.Setup(x => x.GetChatMembershipAsync(sessionId, It.IsAny<CancellationToken>(), It.IsAny<bool>()))
			.ReturnsAsync(TelegramOperationResult<TelegramJoinedChats>.Failed(TelegramOperationStatus.FloodWait));

		await ConsumeAsync();

		VerifyAccountBanNotRecorded();
		storage.Verify(s => s.MarkChatWriteForbiddenAsync(
			It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task NotRecordChatRestriction_WhenAccountIsBanned()
	{
		SetupSend(TelegramOperationResult<int?>.Failed(TelegramOperationStatus.ChannelBanned, "CHAT_RESTRICTED"));

		await ConsumeAsync();

		storage.Verify(s => s.MarkChatWriteForbiddenAsync(
			It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	private Task ConsumeAsync()
	{
		var context = new Mock<ConsumeContext<SendCommentCommand>>();
		context.SetupGet(x => x.Message).Returns(new SendCommentCommand
		{
			CommentRepostSettingsId = settingsId,
			OriginalPostId = OriginalPostId,
			WatchedChannelId = 888,
			WatchedChannelAccessHash = 1,
			DiscussionGroupId = DiscussionGroupId,
			DiscussionGroupAccessHash = 1,
			SourceChannelId = SourceChannelId,
			TelegramSessionId = sessionId
		});
		context.SetupGet(x => x.CancellationToken).Returns(CancellationToken.None);

		return sut.Consume(context.Object);
	}

	private void SetupMembership(bool isAccountRestricted) =>
		tgMessages.Setup(x => x.GetChatMembershipAsync(sessionId, It.IsAny<CancellationToken>(), It.IsAny<bool>()))
			.ReturnsAsync(TelegramOperationResult<TelegramJoinedChats>.Success(new TelegramJoinedChats
			{
				Chats = new Dictionary<long, TelegramChatInfo>
				{
					[DiscussionGroupId] = new()
					{
						Id = DiscussionGroupId,
						AccessHash = 1,
						Title = "discussion",
						IsGroup = true,
						IsAccountRestricted = isAccountRestricted,
						Peer = TelegramPeer.Channel(DiscussionGroupId, 1)
					}
				},
				ForbiddenChatIds = new HashSet<long>()
			}));

	private void SetupSend(TelegramOperationResult<int?> result) =>
		tgMessages.Setup(x => x.SendMessageAsync(
				sessionId, It.IsAny<TelegramPeer>(), It.IsAny<string>(), It.IsAny<int?>(),
				It.IsAny<CancellationToken>(), It.IsAny<bool>()))
			.ReturnsAsync(result);

	private void VerifyAccountBanNotRecorded() =>
		storage.Verify(s => s.MarkAccountBannedAsync(
			It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
}
