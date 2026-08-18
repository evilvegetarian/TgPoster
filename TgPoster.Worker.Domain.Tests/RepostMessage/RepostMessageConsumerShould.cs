using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shared.Enums;
using Shouldly;
using TgPoster.Telegram.Abstractions;
using TgPoster.Telegram.Models;
using TgPoster.Worker.Domain.UseCases.RepostMessageConsumer;

namespace TgPoster.Worker.Domain.Tests.RepostMessage;

public class RepostMessageConsumerShould
{
	private const long DestinationChatId = 555;
	private const int SourceTelegramMessageId = 42;
	private const string SourceChannel = "@sourcechan";
	private readonly Guid destinationId = Guid.NewGuid();
	private readonly List<RepostLogEntry> loggedEntries = [];
	private readonly Guid messageId = Guid.NewGuid();
	private readonly Guid sessionId = Guid.NewGuid();
	private readonly Guid settingsId = Guid.NewGuid();
	private readonly Mock<IRepostMessageConsumerStorage> storage;
	private readonly RepostMessageConsumer sut;
	private readonly Mock<ITelegramMessageService> tgMessages;

	public RepostMessageConsumerShould()
	{
		storage = new Mock<IRepostMessageConsumerStorage>();
		tgMessages = new Mock<ITelegramMessageService>();

		storage.Setup(s => s.CreateRepostLogsAsync(
				It.IsAny<IReadOnlyCollection<RepostLogEntry>>(), It.IsAny<CancellationToken>()))
			.Callback<IReadOnlyCollection<RepostLogEntry>, CancellationToken>((entries, _) =>
				loggedEntries.AddRange(entries))
			.Returns(Task.CompletedTask);

		storage.Setup(s => s.IncrementRepostCounterAsync(destinationId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(1);
		storage.Setup(s => s.GetTodayRepostCountAsync(destinationId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(0);

		SetupRepostData();
		SetupDialogs(DestinationChatId);
		SetupSourceChannel(TelegramOperationResult<TelegramChatInfo>.Success(ChatInfo(777)));
		SetupForward(TelegramOperationResult<int?>.Success(999));

		sut = new RepostMessageConsumer(
			storage.Object,
			tgMessages.Object,
			NullLogger<RepostMessageConsumer>.Instance);
	}

	[Fact]
	public async Task WriteSuccessLog_WhenForwardSucceeds()
	{
		await ConsumeAsync();

		var entry = loggedEntries.ShouldHaveSingleItem();
		entry.RepostDestinationId.ShouldBe(destinationId);
		entry.Status.ShouldBe(RepostStatus.Success);
		entry.Reason.ShouldBe(RepostLogReason.None);
		entry.TelegramMessageId.ShouldBe(999);
		entry.Error.ShouldBeNull();
	}

	[Fact]
	public async Task WriteBannedLogAndDisableDestination_WhenAccountBanned()
	{
		SetupForward(TelegramOperationResult<int?>.Failed(
			TelegramOperationStatus.ChannelBanned, "CHANNEL_BANNED"));

		await ConsumeAsync();

		var entry = loggedEntries.ShouldHaveSingleItem();
		entry.Status.ShouldBe(RepostStatus.Failed);
		entry.Reason.ShouldBe(RepostLogReason.Banned);
		entry.Error.ShouldBe("CHANNEL_BANNED");
		storage.Verify(s => s.UpdateDestinationStatusAsync(
			destinationId, ChatStatus.Banned, false, It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task WriteForwardFailedLog_WhenTelegramReturnsError()
	{
		SetupForward(TelegramOperationResult<int?>.Failed(
			TelegramOperationStatus.UnknownError, "BOOM"));

		await ConsumeAsync();

		var entry = loggedEntries.ShouldHaveSingleItem();
		entry.Status.ShouldBe(RepostStatus.Failed);
		entry.Reason.ShouldBe(RepostLogReason.ForwardFailed);
		entry.Error.ShouldBe("BOOM");
	}

	[Fact]
	public async Task WriteDialogsUnavailableLog_WhenDialogsCannotBeLoaded()
	{
		tgMessages.Setup(x => x.GetAllDialogsAsync(
				sessionId, It.IsAny<CancellationToken>(), It.IsAny<bool>()))
			.ReturnsAsync(TelegramOperationResult<IReadOnlyList<TelegramChatInfo>>.Failed(
				TelegramOperationStatus.SessionNotFound, "сессия не найдена"));

		await ConsumeAsync();

		var entry = loggedEntries.ShouldHaveSingleItem();
		entry.Status.ShouldBe(RepostStatus.Failed);
		entry.Reason.ShouldBe(RepostLogReason.DialogsUnavailable);
	}

	[Fact]
	public async Task WriteSourceChannelNotResolvedLog_WhenSourceChannelIsNotFound()
	{
		SetupSourceChannel(TelegramOperationResult<TelegramChatInfo>.Failed(
			TelegramOperationStatus.UsernameNotFound));

		await ConsumeAsync();

		var entry = loggedEntries.ShouldHaveSingleItem();
		entry.Status.ShouldBe(RepostStatus.Failed);
		entry.Reason.ShouldBe(RepostLogReason.SourceChannelNotResolved);
	}

	[Fact]
	public async Task WriteDestinationNotAvailableLog_WhenDestinationIsMissingInDialogs()
	{
		SetupDialogs(DestinationChatId + 1);

		await ConsumeAsync();

		var entry = loggedEntries.ShouldHaveSingleItem();
		entry.Status.ShouldBe(RepostStatus.Failed);
		entry.Reason.ShouldBe(RepostLogReason.DestinationNotAvailable);
		tgMessages.Verify(x => x.ForwardMessageAsync(
			It.IsAny<Guid>(), It.IsAny<TelegramPeer>(), It.IsAny<TelegramPeer>(), It.IsAny<int>(),
			It.IsAny<CancellationToken>(), It.IsAny<bool>()), Times.Never);
	}

	[Fact]
	public async Task WriteMessageNotPublishedLog_WhenSourceMessageHasNoTelegramId()
	{
		SetupRepostData(null);

		await ConsumeAsync();

		var entry = loggedEntries.ShouldHaveSingleItem();
		entry.Status.ShouldBe(RepostStatus.Failed);
		entry.Reason.ShouldBe(RepostLogReason.MessageNotPublished);
	}

	[Fact]
	public async Task WriteSkippedLog_WhenDailyLimitIsReached()
	{
		SetupRepostData(maxRepostsPerDay: 3);
		storage.Setup(s => s.GetTodayRepostCountAsync(destinationId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(3);

		await ConsumeAsync();

		var entry = loggedEntries.ShouldHaveSingleItem();
		entry.Status.ShouldBe(RepostStatus.Skipped);
		entry.Reason.ShouldBe(RepostLogReason.DailyLimit);
		entry.Error!.ShouldContain("3");
	}

	[Fact]
	public async Task WriteSkippedLog_WhenEveryNthSettingSkipsMessage()
	{
		SetupRepostData(repostEveryNth: 3);

		await ConsumeAsync();

		var entry = loggedEntries.ShouldHaveSingleItem();
		entry.Status.ShouldBe(RepostStatus.Skipped);
		entry.Reason.ShouldBe(RepostLogReason.EveryNth);
	}

	[Fact]
	public async Task NotWriteAnyLog_WhenSettingsAreNotFound()
	{
		storage.Setup(s => s.GetRepostDataAsync(messageId, settingsId, It.IsAny<CancellationToken>()))
			.ReturnsAsync((RepostDataDto?)null);

		await ConsumeAsync();

		loggedEntries.ShouldBeEmpty();
	}

	private Task ConsumeAsync()
	{
		var context = new Mock<ConsumeContext<RepostMessageCommand>>();
		context.SetupGet(x => x.Message).Returns(new RepostMessageCommand
		{
			MessageId = messageId,
			ScheduleId = Guid.NewGuid(),
			RepostSettingsId = settingsId
		});
		context.SetupGet(x => x.CancellationToken).Returns(CancellationToken.None);

		return sut.Consume(context.Object);
	}

	private void SetupRepostData(
		int? telegramMessageId = SourceTelegramMessageId,
		int repostEveryNth = 1,
		int? maxRepostsPerDay = null
	) =>
		storage.Setup(s => s.GetRepostDataAsync(messageId, settingsId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new RepostDataDto
			{
				TelegramMessageId = telegramMessageId,
				TelegramSessionId = sessionId,
				SourceChannelIdentifier = SourceChannel,
				Destinations =
				[
					new RepostDestinationDataDto
					{
						Id = destinationId,
						ChatIdentifier = DestinationChatId,
						RepostEveryNth = repostEveryNth,
						MaxRepostsPerDay = maxRepostsPerDay
					}
				]
			});

	private void SetupDialogs(long chatId) =>
		tgMessages.Setup(x => x.GetAllDialogsAsync(
				sessionId, It.IsAny<CancellationToken>(), It.IsAny<bool>()))
			.ReturnsAsync(TelegramOperationResult<IReadOnlyList<TelegramChatInfo>>.Success([ChatInfo(chatId)]));

	private void SetupSourceChannel(TelegramOperationResult<TelegramChatInfo> result) =>
		tgMessages.Setup(x => x.ResolveChannelAsync(
				sessionId, "sourcechan", It.IsAny<CancellationToken>(), It.IsAny<bool>()))
			.ReturnsAsync(result);

	private void SetupForward(TelegramOperationResult<int?> result) =>
		tgMessages.Setup(x => x.ForwardMessageAsync(
				sessionId, It.IsAny<TelegramPeer>(), It.IsAny<TelegramPeer>(), It.IsAny<int>(),
				It.IsAny<CancellationToken>(), It.IsAny<bool>()))
			.ReturnsAsync(result);

	private static TelegramChatInfo ChatInfo(long id) => new()
	{
		Id = id,
		AccessHash = 1,
		Title = $"chat-{id}",
		Peer = TelegramPeer.Channel(id, 1)
	};
}
