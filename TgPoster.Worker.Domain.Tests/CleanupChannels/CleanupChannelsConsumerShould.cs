using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shared.Enums;
using Shared.Telegram;
using TgPoster.Telegram.Abstractions;
using TgPoster.Telegram.Models;
using TgPoster.Worker.Domain.ConfigModels;
using TgPoster.Worker.Domain.UseCases.CleanupChannels;

namespace TgPoster.Worker.Domain.Tests.CleanupChannels;

public sealed class CleanupChannelsConsumerShould
{
	private readonly Guid jobId = Guid.NewGuid();
	private readonly Mock<ITelegramMessageService> messageService;
	private readonly Guid sessionId = Guid.NewGuid();
	private readonly Mock<ICleanupChannelsStorage> storage;
	private readonly CleanupChannelsConsumer sut;

	public CleanupChannelsConsumerShould()
	{
		storage = new Mock<ICleanupChannelsStorage>();
		messageService = new Mock<ITelegramMessageService>();

		SetupJob();
		storage.Setup(s => s.GetUsedChatsAsync(sessionId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new UsedTelegramChats([], []));

		sut = CreateSut();
	}

	[Fact]
	public async Task LeaveChannel_AndCompleteJob()
	{
		var item = CreateItem(100);
		SetupPendingItems(item);
		SetupLeave(100, TelegramOperationResult<bool>.Success(true));

		await ConsumeAsync();

		messageService.Verify(m => m.LeaveChatAsync(
			sessionId, TelegramPeer.Channel(100, 1000), It.IsAny<CancellationToken>()), Times.Once);
		VerifyItemOutcome(item, ChannelCleanupOutcome.Left);
		VerifyJobStatus(ChannelCleanupStatus.Completed);
	}

	[Fact]
	public async Task LeaveBasicGroup_ByChatPeer()
	{
		var item = CreateItem(200, isBasicGroup: true);
		SetupPendingItems(item);
		SetupLeave(200, TelegramOperationResult<bool>.Success(true));

		await ConsumeAsync();

		messageService.Verify(m => m.LeaveChatAsync(
			sessionId, TelegramPeer.Chat(200), It.IsAny<CancellationToken>()), Times.Once);
		VerifyItemOutcome(item, ChannelCleanupOutcome.Left);
	}

	[Fact]
	public async Task ReportNotMember_WhenAccountAlreadyLeft()
	{
		var item = CreateItem(100);
		SetupPendingItems(item);
		SetupLeave(100, TelegramOperationResult<bool>.Success(false));

		await ConsumeAsync();

		VerifyItemOutcome(item, ChannelCleanupOutcome.NotMember);
	}

	[Fact]
	public async Task KeepChannel_ThatBecameUsedWhileJobWaited()
	{
		var item = CreateItem(100, "nowused");
		SetupPendingItems(item);
		storage.Setup(s => s.GetUsedChatsAsync(sessionId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new UsedTelegramChats([], ["@nowused"]));

		await ConsumeAsync();

		VerifyItemOutcome(item, ChannelCleanupOutcome.InUse);
		messageService.Verify(m => m.LeaveChatAsync(
			It.IsAny<Guid>(), It.IsAny<TelegramPeer>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task PauseAndKeepChannelQueued_WhenTelegramRateLimits()
	{
		var first = CreateItem(100);
		var second = CreateItem(200);
		SetupPendingItems(first, second);
		SetupLeave(100, TelegramOperationResult<bool>.Failed(TelegramOperationStatus.FloodWait, "FLOOD_WAIT_X", 600));

		await ConsumeAsync();

		storage.Verify(s => s.SetSessionFloodWaitAsync(
			sessionId,
			It.Is<DateTimeOffset>(x => x > DateTimeOffset.UtcNow.AddMinutes(9)),
			It.IsAny<CancellationToken>()), Times.Once);
		VerifyJobStatus(ChannelCleanupStatus.CooldownWait);
		storage.Verify(s => s.UpdateItemAsync(
			It.IsAny<Guid>(), It.IsAny<ChannelCleanupOutcome>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
			Times.Never);
		messageService.Verify(m => m.LeaveChatAsync(
			sessionId, TelegramPeer.Channel(200, 2000), It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task UseDefaultCooldown_WhenTelegramDoesNotReportDuration()
	{
		SetupPendingItems(CreateItem(100));
		SetupLeave(100, TelegramOperationResult<bool>.Failed(TelegramOperationStatus.SpamRestricted, "PEER_FLOOD"));

		await ConsumeAsync();

		storage.Verify(s => s.SetSessionFloodWaitAsync(
			sessionId,
			It.Is<DateTimeOffset>(x => x > DateTimeOffset.UtcNow.AddMinutes(50)),
			It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task StopJob_WhenSessionIsGone()
	{
		SetupPendingItems(CreateItem(100), CreateItem(200));
		SetupLeave(100, TelegramOperationResult<bool>.Failed(TelegramOperationStatus.SessionNotFound, "revoked"));

		await ConsumeAsync();

		storage.Verify(s => s.MarkPendingItemsNotProcessedAsync(
			jobId, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
		VerifyJobStatus(ChannelCleanupStatus.Failed);
		messageService.Verify(m => m.LeaveChatAsync(
			sessionId, TelegramPeer.Channel(200, 2000), It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task ContinueWithNextChannel_AfterFailedOne()
	{
		var first = CreateItem(100);
		var second = CreateItem(200);
		SetupPendingItems(first, second);
		SetupLeave(100, TelegramOperationResult<bool>.Failed(TelegramOperationStatus.UnknownError, "CHAT_ADMIN_REQUIRED"));
		SetupLeave(200, TelegramOperationResult<bool>.Success(true));

		await ConsumeAsync();

		storage.Verify(s => s.UpdateItemAsync(
			first.ItemId, ChannelCleanupOutcome.Failed, "CHAT_ADMIN_REQUIRED", It.IsAny<CancellationToken>()),
			Times.Once);
		VerifyItemOutcome(second, ChannelCleanupOutcome.Left);
		VerifyJobStatus(ChannelCleanupStatus.Completed);
	}

	[Fact]
	public async Task NotTouchTelegram_WhenSessionIsUnderFloodWait()
	{
		SetupJob(DateTimeOffset.UtcNow.AddMinutes(10));
		SetupPendingItems(CreateItem(100));

		await ConsumeAsync();

		messageService.Verify(m => m.LeaveChatAsync(
			It.IsAny<Guid>(), It.IsAny<TelegramPeer>(), It.IsAny<CancellationToken>()), Times.Never);
		VerifyJobStatus(ChannelCleanupStatus.CooldownWait);
	}

	[Fact]
	public async Task DoNothing_WhenJobAlreadyFinished()
	{
		SetupJob(status: ChannelCleanupStatus.Completed);

		await ConsumeAsync();

		storage.Verify(s => s.GetPendingItemsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task ProcessOnlyOneBatch_AndRequeueRemainingChannels()
	{
		var first = CreateItem(100);
		var second = CreateItem(200);
		SetupPendingItems(first, second);
		SetupLeave(100, TelegramOperationResult<bool>.Success(true));

		var context = await ConsumeAsync(CreateSut(1));

		VerifyItemOutcome(first, ChannelCleanupOutcome.Left);
		messageService.Verify(m => m.LeaveChatAsync(
			sessionId, TelegramPeer.Channel(200, 2000), It.IsAny<CancellationToken>()), Times.Never);
		context.Verify(c => c.Publish(
			It.Is<CleanupChannelsContract>(x => x.JobId == jobId), It.IsAny<CancellationToken>()), Times.Once);
		storage.Verify(s => s.SetJobStatusAsync(
			jobId, ChannelCleanupStatus.Completed, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	private CleanupChannelsConsumer CreateSut(int maxItemsPerRun = 100) =>
		new(
			storage.Object,
			messageService.Object,
			new ChannelCleanupOptions
			{
				MinDelaySeconds = 0,
				MaxDelaySeconds = 0,
				MaxItemsPerRun = maxItemsPerRun
			},
			NullLogger<CleanupChannelsConsumer>.Instance);

	private async Task<Mock<ConsumeContext<CleanupChannelsContract>>> ConsumeAsync(
		CleanupChannelsConsumer? consumer = null
	)
	{
		var context = new Mock<ConsumeContext<CleanupChannelsContract>>();
		context.SetupGet(x => x.Message).Returns(new CleanupChannelsContract { JobId = jobId });
		context.SetupGet(x => x.CancellationToken).Returns(CancellationToken.None);

		await (consumer ?? sut).Consume(context.Object);

		return context;
	}

	private void SetupJob(
		DateTimeOffset? floodWaitUntil = null,
		ChannelCleanupStatus status = ChannelCleanupStatus.Pending
	) =>
		storage.Setup(s => s.GetJobAsync(jobId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new CleanupJobData(jobId, sessionId, floodWaitUntil, status));

	private static CleanupJobPendingItem CreateItem(long chatId, string? username = null, bool isBasicGroup = false) =>
		new(Guid.NewGuid(), chatId, isBasicGroup ? 0 : chatId * 10, isBasicGroup, username);

	private void SetupPendingItems(params CleanupJobPendingItem[] items) =>
		storage.Setup(s => s.GetPendingItemsAsync(jobId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(items.ToList());

	private void SetupLeave(long chatId, TelegramOperationResult<bool> result) =>
		messageService.Setup(m => m.LeaveChatAsync(
				sessionId, It.Is<TelegramPeer>(p => p.Id == chatId), It.IsAny<CancellationToken>()))
			.ReturnsAsync(result);

	private void VerifyItemOutcome(CleanupJobPendingItem item, ChannelCleanupOutcome outcome) =>
		storage.Verify(s => s.UpdateItemAsync(
			item.ItemId, outcome, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);

	private void VerifyJobStatus(ChannelCleanupStatus status) =>
		storage.Verify(s => s.SetJobStatusAsync(
			jobId, status, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
}
