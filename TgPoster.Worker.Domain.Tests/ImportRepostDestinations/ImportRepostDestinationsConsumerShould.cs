using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shared.Enums;
using Shared.Telegram;
using Shouldly;
using TgPoster.Telegram.Abstractions;
using TgPoster.Telegram.Models;
using TgPoster.Worker.Domain.ConfigModels;
using TgPoster.Worker.Domain.UseCases.ImportRepostDestinations;

namespace TgPoster.Worker.Domain.Tests.ImportRepostDestinations;

public class ImportRepostDestinationsConsumerShould
{
	private const long SourceChannelId = 777;
	private readonly Mock<ITelegramChatService> chatService;
	private readonly Guid destinationId = Guid.NewGuid();
	private readonly Guid jobId = Guid.NewGuid();
	private readonly Guid sessionId = Guid.NewGuid();
	private readonly Guid settingsId = Guid.NewGuid();
	private readonly Mock<IImportRepostDestinationsStorage> storage;
	private readonly ImportRepostDestinationsConsumer sut;

	public ImportRepostDestinationsConsumerShould()
	{
		storage = new Mock<IImportRepostDestinationsStorage>();
		chatService = new Mock<ITelegramChatService>();

		SetupJob();

		storage.Setup(s => s.GetExistingChatIdsAsync(settingsId, It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);

		storage.Setup(s => s.AddDestinationAsync(
				It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<string?>(), It.IsAny<string?>(),
				It.IsAny<int?>(), It.IsAny<ChatType>(), It.IsAny<Guid>(), It.IsAny<int>(),
				It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int?>(),
				It.IsAny<CancellationToken>()))
			.ReturnsAsync(destinationId);

		sut = CreateSut();
	}

	/// <summary>
	///     Нулевые задержки: тест не должен спать между каналами
	/// </summary>
	/// <param name="maxItemsPerRun">Сколько каналов обрабатывать за проход</param>
	/// <returns>Консьюмер с подменёнными зависимостями</returns>
	private ImportRepostDestinationsConsumer CreateSut(int maxItemsPerRun = 100) =>
		new(
			storage.Object,
			chatService.Object,
			new RepostImportOptions
			{
				MinDelaySeconds = 0,
				MaxDelaySeconds = 0,
				MaxItemsPerRun = maxItemsPerRun
			},
			NullLogger<ImportRepostDestinationsConsumer>.Instance);

	[Fact]
	public async Task AddChannelWithDefaultsFromSettings()
	{
		var item = SetupPendingItem(username: "targetchan");
		SetupChat(item, 12345, true, true);

		await ConsumeAsync();

		storage.Verify(s => s.AddDestinationAsync(
			settingsId, 12345, It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int?>(),
			ChatType.Channel, item.DiscoveredChannelId, 10, 60, 2, 30, 5,
			It.IsAny<CancellationToken>()), Times.Once);
		VerifyItemOutcome(item, AddDestinationOutcome.Added);
		VerifyJobStatus(RepostImportStatus.Completed);
	}

	[Fact]
	public async Task SkipChannelBannedFromWriting_WithoutCallingTelegram()
	{
		var item = SetupPendingItem(username: "readonlychan", canSendMessages: false);

		await ConsumeAsync();

		VerifyItemOutcome(item, AddDestinationOutcome.NoWritePermission);
		chatService.Verify(c => c.TryGetChatInfoAsync(
			It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
	}

	[Fact]
	public async Task SkipChannelBannedFromMedia_WithoutCallingTelegram()
	{
		var item = SetupPendingItem(username: "nomediachan", canSendMedia: false);

		await ConsumeAsync();

		VerifyItemOutcome(item, AddDestinationOutcome.NoMediaPermission);
		chatService.Verify(c => c.TryGetChatInfoAsync(
			It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
	}

	[Fact]
	public async Task NotTouchTelegram_WhenSessionIsUnderFloodWait()
	{
		SetupJob(floodWaitUntil: DateTimeOffset.UtcNow.AddMinutes(10));
		var item = SetupPendingItem(username: "targetchan");
		SetupChat(item, 12345, true, true);

		await ConsumeAsync();

		chatService.Verify(c => c.TryGetChatInfoAsync(
			It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
		VerifyJobStatus(RepostImportStatus.CooldownWait);
	}

	[Fact]
	public async Task PauseJobAndRememberCooldown_WhenTelegramRateLimits()
	{
		var first = SetupPendingItem(username: "floodchan");
		var second = SetupPendingItem(username: "secondchan");
		SetupPendingItems(first, second);

		chatService.Setup(c => c.TryGetChatInfoAsync(sessionId, "@floodchan", It.IsAny<bool>()))
			.ReturnsAsync(TelegramOperationResult<TelegramChatInfo>.Failed(
				TelegramOperationStatus.FloodWait, "FLOOD_WAIT_600", 600));

		await ConsumeAsync();

		storage.Verify(s => s.SetSessionFloodWaitAsync(
			sessionId, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Once);
		VerifyItemOutcome(first, AddDestinationOutcome.RateLimited);
		VerifyJobStatus(RepostImportStatus.CooldownWait);
		chatService.Verify(c => c.TryGetChatInfoAsync(sessionId, "@secondchan", It.IsAny<bool>()), Times.Never);
		storage.Verify(s => s.UpdateItemAsync(
			second.ItemId, It.IsAny<AddDestinationOutcome>(), It.IsAny<Guid?>(), It.IsAny<string?>(),
			It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task UseDefaultCooldown_WhenTelegramDoesNotReportDuration()
	{
		var item = SetupPendingItem(username: "peerfloodchan");
		chatService.Setup(c => c.TryGetChatInfoAsync(sessionId, "@peerfloodchan", It.IsAny<bool>()))
			.ReturnsAsync(TelegramOperationResult<TelegramChatInfo>.Failed(
				TelegramOperationStatus.SpamRestricted, "PEER_FLOOD"));

		await ConsumeAsync();

		storage.Verify(s => s.SetSessionFloodWaitAsync(
			sessionId,
			It.Is<DateTimeOffset>(x => x > DateTimeOffset.UtcNow.AddMinutes(50)),
			It.IsAny<CancellationToken>()), Times.Once);
		VerifyItemOutcome(item, AddDestinationOutcome.RateLimited);
	}

	[Fact]
	public async Task RecordPermissionsInDiscover_EvenWhenChannelRejected()
	{
		var item = SetupPendingItem(username: "readonlychan");
		SetupChat(item, 4242, true, false);

		await ConsumeAsync();

		storage.Verify(s => s.UpdateDiscoveredChannelAsync(
			item.DiscoveredChannelId, 4242, It.IsAny<string?>(), It.IsAny<string?>(), ChatType.Channel,
			true, false, It.IsAny<CancellationToken>()), Times.Once);
		VerifyItemOutcome(item, AddDestinationOutcome.NoMediaPermission);
	}

	[Fact]
	public async Task SkipSourceChannel_WhenResolvedIdMatchesSchedule()
	{
		var item = SetupPendingItem(username: "sourcechan");
		SetupChat(item, SourceChannelId, true, true);

		await ConsumeAsync();

		VerifyItemOutcome(item, AddDestinationOutcome.SourceChannel);
		VerifyNoDestinationAdded();
	}

	[Fact]
	public async Task SkipAlreadyAddedChannel_WhenResolvedIdIsKnown()
	{
		var item = SetupPendingItem(username: "dupechan");
		SetupChat(item, 555, true, true);
		storage.Setup(s => s.GetExistingChatIdsAsync(settingsId, It.IsAny<CancellationToken>()))
			.ReturnsAsync([555]);

		await ConsumeAsync();

		VerifyItemOutcome(item, AddDestinationOutcome.AlreadyAdded);
		VerifyNoDestinationAdded();
	}

	[Fact]
	public async Task ReportNotResolved_WhenTelegramCannotFindChannel()
	{
		var item = SetupPendingItem(username: "ghostchan");
		chatService.Setup(c => c.TryGetChatInfoAsync(sessionId, "@ghostchan", It.IsAny<bool>()))
			.ReturnsAsync(TelegramOperationResult<TelegramChatInfo>.Failed(
				TelegramOperationStatus.UsernameNotFound, "USERNAME_NOT_OCCUPIED"));

		await ConsumeAsync();

		VerifyItemOutcome(item, AddDestinationOutcome.NotResolved);
		VerifyJobStatus(RepostImportStatus.Completed);
	}

	[Fact]
	public async Task ContinueWithNextChannel_AfterUnresolvedOne()
	{
		var first = SetupPendingItem(username: "ghostchan");
		var second = SetupPendingItem(username: "goodchan");
		SetupPendingItems(first, second);

		chatService.Setup(c => c.TryGetChatInfoAsync(sessionId, "@ghostchan", It.IsAny<bool>()))
			.ReturnsAsync(TelegramOperationResult<TelegramChatInfo>.Failed(
				TelegramOperationStatus.UsernameNotFound, "USERNAME_NOT_OCCUPIED"));
		SetupChat(second, 8888, true, true);

		await ConsumeAsync();

		VerifyItemOutcome(second, AddDestinationOutcome.Added);
		VerifyJobStatus(RepostImportStatus.Completed);
	}

	[Fact]
	public async Task ResolvePrivateChannelByInviteHash()
	{
		var item = SetupPendingItem(inviteHash: "AbCdEf");
		SetupChat(item, 9999, true, true, "https://t.me/+AbCdEf");

		await ConsumeAsync();

		chatService.Verify(c => c.TryGetChatInfoAsync(sessionId, "https://t.me/+AbCdEf", It.IsAny<bool>()),
			Times.Once);
		VerifyItemOutcome(item, AddDestinationOutcome.Added);
	}

	[Fact]
	public async Task PassAutoJoinFlagFromJob()
	{
		SetupJob(autoJoin: false);
		var item = SetupPendingItem(username: "targetchan");
		SetupChat(item, 12345, true, true);

		await ConsumeAsync();

		chatService.Verify(c => c.TryGetChatInfoAsync(sessionId, "@targetchan", false), Times.Once);
	}

	[Fact]
	public async Task DoNothing_WhenJobAlreadyCompleted()
	{
		SetupJob(status: RepostImportStatus.Completed);

		await ConsumeAsync();

		storage.Verify(s => s.GetPendingItemsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
		chatService.Verify(c => c.TryGetChatInfoAsync(
			It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
	}

	[Fact]
	public async Task CompleteJob_WhenNothingLeftToProcess()
	{
		SetupPendingItems();

		await ConsumeAsync();

		VerifyJobStatus(RepostImportStatus.Completed);
	}

	[Fact]
	public async Task ProcessOnlyOneBatch_AndRequeueRemainingChannels()
	{
		var first = SetupPendingItem(username: "firstchan");
		var second = SetupPendingItem(username: "secondchan");
		SetupPendingItems(first, second);
		SetupChat(first, 1111, true, true);
		SetupChat(second, 2222, true, true);

		var context = await ConsumeAsync(CreateSut(1));

		VerifyItemOutcome(first, AddDestinationOutcome.Added);
		storage.Verify(s => s.UpdateItemAsync(
			second.ItemId, It.IsAny<AddDestinationOutcome>(), It.IsAny<Guid?>(), It.IsAny<string?>(),
			It.IsAny<CancellationToken>()), Times.Never);
		context.Verify(c => c.Publish(
			It.Is<ImportRepostDestinationsContract>(x => x.JobId == jobId),
			It.IsAny<CancellationToken>()), Times.Once);
		storage.Verify(s => s.SetJobStatusAsync(
			jobId, RepostImportStatus.Completed, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task NotRequeueJob_WhenBatchCoversAllRemainingChannels()
	{
		var item = SetupPendingItem(username: "targetchan");
		SetupChat(item, 12345, true, true);

		var context = await ConsumeAsync(CreateSut(1));

		VerifyJobStatus(RepostImportStatus.Completed);
		context.Verify(c => c.Publish(
			It.IsAny<ImportRepostDestinationsContract>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	private async Task<Mock<ConsumeContext<ImportRepostDestinationsContract>>> ConsumeAsync(
		ImportRepostDestinationsConsumer? consumer = null
	)
	{
		var context = new Mock<ConsumeContext<ImportRepostDestinationsContract>>();
		context.SetupGet(x => x.Message).Returns(new ImportRepostDestinationsContract { JobId = jobId });
		context.SetupGet(x => x.CancellationToken).Returns(CancellationToken.None);

		await (consumer ?? sut).Consume(context.Object);

		return context;
	}

	private void SetupJob(
		DateTimeOffset? floodWaitUntil = null,
		bool autoJoin = true,
		RepostImportStatus status = RepostImportStatus.Pending
	) =>
		storage.Setup(s => s.GetJobAsync(jobId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new ImportJobData(
				jobId,
				settingsId,
				sessionId,
				SourceChannelId,
				floodWaitUntil,
				autoJoin,
				status,
				10,
				60,
				2,
				30,
				5));

	private ImportJobPendingItem SetupPendingItem(
		string? username = null,
		string? inviteHash = null,
		long? telegramId = null,
		bool? canSendMessages = null,
		bool? canSendMedia = null
	)
	{
		var item = new ImportJobPendingItem(
			Guid.NewGuid(),
			Guid.NewGuid(),
			telegramId,
			username,
			inviteHash,
			1000,
			canSendMessages,
			canSendMedia);
		SetupPendingItems(item);

		return item;
	}

	private void SetupPendingItems(params ImportJobPendingItem[] items) =>
		storage.Setup(s => s.GetPendingItemsAsync(jobId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(items.ToList());

	private void SetupChat(
		ImportJobPendingItem item,
		long chatId,
		bool canSendMessages,
		bool canSendMedia,
		string? identifier = null
	)
	{
		var info = new TelegramChatInfo
		{
			Id = chatId,
			AccessHash = 1,
			Title = "Тестовый канал",
			Username = item.Username,
			IsChannel = true,
			IsGroup = false,
			CanSendMessages = canSendMessages,
			CanSendMedia = canSendMedia,
			Peer = TelegramPeer.Channel(chatId, 1)
		};

		chatService.Setup(c => c.TryGetChatInfoAsync(
				sessionId,
				identifier ?? "@" + item.Username,
				It.IsAny<bool>()))
			.ReturnsAsync(TelegramOperationResult<TelegramChatInfo>.Success(info));
	}

	private void VerifyItemOutcome(ImportJobPendingItem item, AddDestinationOutcome outcome) =>
		storage.Verify(s => s.UpdateItemAsync(
			item.ItemId, outcome, It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
			Times.Once);

	private void VerifyJobStatus(RepostImportStatus status) =>
		storage.Verify(s => s.SetJobStatusAsync(
			jobId, status, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);

	private void VerifyNoDestinationAdded() =>
		storage.Verify(s => s.AddDestinationAsync(
			It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<string?>(), It.IsAny<string?>(),
			It.IsAny<int?>(), It.IsAny<ChatType>(), It.IsAny<Guid>(), It.IsAny<int>(),
			It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int?>(),
			It.IsAny<CancellationToken>()), Times.Never);
}
