using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shared.Discover;
using Shared.Enums;
using Shouldly;
using TgPoster.Telegram.Abstractions;
using TgPoster.Telegram.Models;
using TgPoster.Worker.Domain.UseCases.DiscoverChannelLinks;
using TgPoster.Worker.Domain.UseCases.WorkerJobStatus;

namespace TgPoster.Worker.Domain.Tests.DiscoverChannelLinks;

public class DiscoverChannelLinksWorkerShould
{
	private static readonly DateTimeOffset FixedNow = new(2026, 10, 26, 12, 0, 0, TimeSpan.Zero);

	private readonly Mock<IDiscoverChannelLinksStorage> storage = new();
	private readonly Mock<ITelegramAuthService> authService = new();
	private readonly Mock<ITelegramMessageService> tgMessages = new();
	private readonly Mock<ITelegramPublicLookupService> publicLookup = new();
	private readonly Mock<IWorkerJobStatusStorage> statusStorage = new();
	private readonly Guid firstSessionId = Guid.NewGuid();
	private readonly Guid secondSessionId = Guid.NewGuid();
	private readonly DiscoverChannelDto firstChannel = new() { Id = Guid.NewGuid(), Username = "first_chat" };
	private readonly DiscoverChannelDto secondChannel = new() { Id = Guid.NewGuid(), Username = "second_chat" };

	private DiscoverSettingsDto settings = new() { IsEnabled = true, IntervalMinutes = 60, ChannelsPerSession = 1 };

	public DiscoverChannelLinksWorkerShould()
	{
		storage.Setup(s => s.GetSettingsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(() => settings);
		storage.Setup(s => s.GetKnownUsernamesAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);
		SetupSessions(firstSessionId);
		SetupQueue(firstChannel);
	}

	[Fact]
	public async Task SkipRun_WhenDisabled()
	{
		settings = settings with { IsEnabled = false };
		var sut = CreateSut();

		await sut.ProcessChannelsAsync();

		VerifyNeverStarted();
		VerifyQueueNeverRequested();
	}

	[Fact]
	public async Task SkipRun_WhenIntervalHasNotElapsed()
	{
		SetupLastStartedAt(FixedNow.AddMinutes(-30));
		var sut = CreateSut();

		await sut.ProcessChannelsAsync();

		VerifyNeverStarted();
		VerifyQueueNeverRequested();
	}

	[Fact]
	public async Task Run_WhenIntervalHasElapsedWithinTolerance()
	{
		SetupLastStartedAt(FixedNow.AddMinutes(-60).AddSeconds(10));
		var sut = CreateSut();

		await sut.ProcessChannelsAsync();

		statusStorage.Verify(s => s.ReportStartedAsync(
			WorkerJobNames.DiscoverChannelLinks, It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task ReportNextRun_AfterConfiguredInterval()
	{
		settings = settings with { IntervalMinutes = 15 };
		SetupQueue();
		var sut = CreateSut();

		await sut.ProcessChannelsAsync();

		statusStorage.Verify(s => s.ReportCompletedAsync(
			WorkerJobNames.DiscoverChannelLinks, FixedNow.AddMinutes(15), It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task RequestChannelsPerSessionFromSettings_ForEverySession()
	{
		settings = settings with { ChannelsPerSession = 3 };
		SetupSessions(firstSessionId, secondSessionId);
		var sut = CreateSut();

		await sut.ProcessChannelsAsync();

		storage.Verify(s => s.GetChannelsToProcessAsync(
			6, It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task UseDefaultSettings_WhenNoneSaved()
	{
		storage.Setup(s => s.GetSettingsAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync((DiscoverSettingsDto?)null);
		SetupQueue();
		var sut = CreateSut();

		await sut.ProcessChannelsAsync();

		storage.Verify(s => s.GetChannelsToProcessAsync(
			DiscoverDefaults.ChannelsPerSession, It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()),
			Times.Once);
		statusStorage.Verify(s => s.ReportCompletedAsync(
				WorkerJobNames.DiscoverChannelLinks,
				FixedNow.AddMinutes(DiscoverDefaults.IntervalMinutes),
				It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Theory]
	[InlineData(TelegramOperationStatus.AccessDenied)]
	[InlineData(TelegramOperationStatus.UnknownError)]
	public async Task MarkChannelAsError_WhenChannelCannotBeResolved(TelegramOperationStatus status)
	{
		SetupResolve(firstChannel, TelegramOperationResult<TelegramChatInfo>.Failed(status, "fail"));
		var sut = CreateSut();

		await sut.ProcessChannelsAsync();

		storage.Verify(s => s.MarkAsErrorAsync(firstChannel.Id, It.IsAny<CancellationToken>()), Times.Once);
		statusStorage.Verify(s => s.ReportCompletedAsync(
			WorkerJobNames.DiscoverChannelLinks, It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()), Times.Once);
	}

	[Theory]
	[InlineData(TelegramOperationStatus.Timeout)]
	[InlineData(TelegramOperationStatus.SessionNotFound)]
	[InlineData(TelegramOperationStatus.SpamRestricted)]
	public async Task KeepChannelInQueue_WhenResolveFailsNotBecauseOfChannel(TelegramOperationStatus status)
	{
		SetupResolve(firstChannel, TelegramOperationResult<TelegramChatInfo>.Failed(status, "fail"));
		var sut = CreateSut();

		await sut.ProcessChannelsAsync();

		VerifyNeverMarkedAsError();
		storage.Verify(s => s.ChannelBanned(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task ReportCooldown_AndKeepChannelInQueue_WhenResolveGetsFloodWait()
	{
		SetupResolve(firstChannel,
			TelegramOperationResult<TelegramChatInfo>.Failed(TelegramOperationStatus.FloodWait, "flood", 600));
		var sut = CreateSut();

		await sut.ProcessChannelsAsync();

		VerifyNeverMarkedAsError();
		statusStorage.Verify(s => s.ReportCooldownAsync(
			WorkerJobNames.DiscoverChannelLinks,
			FixedNow.AddSeconds(600),
			FixedNow.AddMinutes(settings.IntervalMinutes),
			It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task MarkChannelAsBanned_NotAsError_WhenUsernameNotFound()
	{
		SetupResolve(firstChannel,
			TelegramOperationResult<TelegramChatInfo>.Failed(TelegramOperationStatus.UsernameNotFound, "gone"));
		var sut = CreateSut();

		await sut.ProcessChannelsAsync();

		storage.Verify(s => s.ChannelBanned(firstChannel.Id, It.IsAny<CancellationToken>()), Times.Once);
		VerifyNeverMarkedAsError();
	}

	[Fact]
	public async Task RecordAccountBan_NotGlobalBanOrError_WhenChannelIsClosedForSession()
	{
		SetupResolve(firstChannel, new TelegramOperationResult<TelegramChatInfo>
		{
			Status = TelegramOperationStatus.ChannelBanned,
			ErrorMessage = "CHANNEL_FORBIDDEN",
			Value = new TelegramChatInfo
			{
				Id = 9001, AccessHash = 1, Title = "closed", Peer = TelegramPeer.Channel(9001, 1)
			}
		});
		var sut = CreateSut();

		await sut.ProcessChannelsAsync();

		storage.Verify(s => s.MarkAccountBannedAsync(
			firstChannel.Id, firstSessionId, 9001, "CHANNEL_FORBIDDEN", It.IsAny<CancellationToken>()), Times.Once);
		storage.Verify(s => s.ChannelBanned(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
		VerifyNeverMarkedAsError();
	}

	[Fact]
	public async Task GiveChannelToAnotherSession_WhenAssignedSessionIsBannedInIt()
	{
		var bannedForFirst = new DiscoverChannelDto
		{
			Id = Guid.NewGuid(),
			Username = "banned_for_first",
			TelegramId = 9001,
			BannedSessionIds = [firstSessionId]
		};
		SetupSessions(firstSessionId, secondSessionId);
		SetupQueue(bannedForFirst);
		SetupResolve(bannedForFirst,
			TelegramOperationResult<TelegramChatInfo>.Failed(TelegramOperationStatus.AccessDenied, "fail"));
		var sut = CreateSut();

		await sut.ProcessChannelsAsync();

		tgMessages.Verify(s => s.ResolveChannelAsync(
			firstSessionId, It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<bool>()), Times.Never);
		tgMessages.Verify(s => s.ResolveChannelAsync(
			secondSessionId, "banned_for_first", It.IsAny<CancellationToken>(), It.IsAny<bool>()), Times.Once);
	}

	[Fact]
	public async Task PassDiscoverSessionsToQueue()
	{
		SetupSessions(firstSessionId, secondSessionId);
		var sut = CreateSut();

		await sut.ProcessChannelsAsync();

		storage.Verify(s => s.GetChannelsToProcessAsync(
			It.IsAny<int>(),
			It.Is<IReadOnlyCollection<Guid>>(ids => ids.SequenceEqual(new[] { firstSessionId, secondSessionId })),
			It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task MarkChannelAsError_AndReportFailure_WhenEveryChannelThrows()
	{
		SetupResolveThrows(firstChannel, "boom");
		var sut = CreateSut();

		await Should.NotThrowAsync(() => sut.ProcessChannelsAsync());

		storage.Verify(s => s.MarkAsErrorAsync(firstChannel.Id, It.IsAny<CancellationToken>()), Times.Once);
		statusStorage.Verify(s => s.ReportFailedAsync(
			WorkerJobNames.DiscoverChannelLinks, "boom", It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task ReportCompleted_WhenOnlySomeChannelsThrow()
	{
		SetupSessions(firstSessionId, secondSessionId);
		SetupQueue(firstChannel, secondChannel);
		SetupResolveThrows(firstChannel, "boom");
		SetupResolve(secondChannel,
			TelegramOperationResult<TelegramChatInfo>.Failed(TelegramOperationStatus.AccessDenied, "private"));
		var sut = CreateSut();

		await sut.ProcessChannelsAsync();

		storage.Verify(s => s.MarkAsErrorAsync(firstChannel.Id, It.IsAny<CancellationToken>()), Times.Once);
		storage.Verify(s => s.MarkAsErrorAsync(secondChannel.Id, It.IsAny<CancellationToken>()), Times.Once);
		statusStorage.Verify(s => s.ReportCompletedAsync(
			WorkerJobNames.DiscoverChannelLinks, It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()), Times.Once);
		statusStorage.Verify(s => s.ReportFailedAsync(
				It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()),
			Times.Never);
	}

	private void SetupSessions(params Guid[] sessionIds) =>
		authService
			.Setup(s => s.GetSessionIdsForPurposeAsync(TelegramSessionPurpose.Discover, It.IsAny<CancellationToken>()))
			.ReturnsAsync([..sessionIds]);

	[Fact]
	public async Task SavePeersFromHistory_WithoutDuplicatingSourceOrKnownUsernames()
	{
		SetupSuccessfulSource();
		var known = Chat(200, "knownchat", "Known");
		SetupHistory(new TelegramHistoryPage
		{
			Chats = [known, Chat(100, firstChannel.Username!, "Source"), Chat(300, null, "Private")],
			Messages = [new TelegramMessage
			{
				Id = 42, Date = FixedNow.UtcDateTime,
				Text = "https://t.me/knownchat @knownchat https://t.me/first_chat https://t.me/c/400/5"
			}]
		});
		List<DiscoveredPeerUpsert>? batch = null;
		storage.Setup(x => x.BulkUpsertAsync(It.IsAny<IReadOnlyCollection<DiscoveredPeerUpsert>>(), It.IsAny<CancellationToken>()))
			.Callback<IReadOnlyCollection<DiscoveredPeerUpsert>, CancellationToken>((items, _) => batch = items.ToList());

		await CreateSut().ProcessChannelsAsync();

		batch.ShouldNotBeNull();
		batch.Count.ShouldBe(3);
		batch.ShouldContain(x => x.Username == "knownchat" && x.TelegramId == 200 && x.Title == "Known");
		batch.ShouldContain(x => x.TelegramId == 300 && x.Username == null);
		batch.ShouldContain(x => x.TelegramId == 400 && x.PeerType == "channel");
		batch.ShouldAllBe(x => x.DiscoveredFromChannelId == firstChannel.Id);
		batch.ShouldNotContain(x => x.Username == firstChannel.Username);
		publicLookup.VerifyNoOtherCalls();
		storage.Verify(x => x.UpsertAsync(It.Is<DiscoveredPeerUpsert>(p => p.MarkAsCompleted && p.LastParsedId == 42),
			It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task LookUpOnlyMissingUsernames_WhenSourceMentionsKnownAndNewChannels()
	{
		SetupSuccessfulSource();
		SetupHistory(new TelegramHistoryPage { Messages = [new TelegramMessage
		{
			Id = 1, Date = FixedNow.UtcDateTime, Text = "@KNOWNCHAT @newchat"
		}] });
		storage.Setup(x => x.GetKnownUsernamesAsync(It.Is<IReadOnlyCollection<string>>(names =>
			names.Count == 2 && names.Contains("KNOWNCHAT") && names.Contains("newchat")), It.IsAny<CancellationToken>()))
			.ReturnsAsync(["KnownChat"]);
		publicLookup.Setup(x => x.LookupAsync("newchat", It.IsAny<CancellationToken>()))
			.ReturnsAsync(TelegramOperationResult<TelegramPublicEntityInfo>.Success(new TelegramPublicEntityInfo
			{
				Type = TelegramEntityType.Group, Username = "newchat"
			}));

		await CreateSut().ProcessChannelsAsync();

		publicLookup.Verify(x => x.LookupAsync("newchat", It.IsAny<CancellationToken>()), Times.Once);
		publicLookup.VerifyNoOtherCalls();
		storage.Verify(x => x.BulkUpsertAsync(It.Is<IReadOnlyCollection<DiscoveredPeerUpsert>>(items =>
			items.Count == 1 && items.Single().Username == "newchat"), It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task ShareLookup_WhenTwoSessionsFindTheSameNewUsername()
	{
		SetupSessions(firstSessionId, secondSessionId);
		SetupQueue(firstChannel, secondChannel);
		SetupSuccessfulSource();
		SetupResolve(secondChannel, TelegramOperationResult<TelegramChatInfo>.Success(Chat(101, secondChannel.Username!, "Other")));
		tgMessages.Setup(x => x.SearchMessagesAsync(It.IsAny<Guid>(), It.IsAny<TelegramPeer>(), TelegramMessageFilter.Url,
			100, 0, 0, 0, "", It.IsAny<CancellationToken>(), true))
			.ReturnsAsync(TelegramOperationResult<TelegramHistoryPage>.Success(new TelegramHistoryPage
			{
				Messages = [new TelegramMessage { Id = 1, Date = FixedNow.UtcDateTime, Text = "@sharedchat" }]
			}));
		publicLookup.Setup(x => x.LookupAsync("sharedchat", It.IsAny<CancellationToken>()))
			.ReturnsAsync(TelegramOperationResult<TelegramPublicEntityInfo>.Success(new TelegramPublicEntityInfo
			{
				Type = TelegramEntityType.Channel, Username = "sharedchat"
			}));

		await CreateSut().ProcessChannelsAsync();

		publicLookup.Verify(x => x.LookupAsync("sharedchat", It.IsAny<CancellationToken>()), Times.Once);
		storage.Verify(x => x.BulkUpsertAsync(It.Is<IReadOnlyCollection<DiscoveredPeerUpsert>>(items =>
			items.Count == 1 && items.Single().Username == "sharedchat"), It.IsAny<CancellationToken>()), Times.Exactly(2));
	}

	[Fact]
	public async Task ReadHistoryPagesUsingLastMessageOffset_AndSaveLargestMessageId()
	{
		SetupSuccessfulSource();
		SetupHistory(new TelegramHistoryPage
		{
			Messages = Enumerable.Range(101, 100).Reverse().Select(id => new TelegramMessage { Id = id, Date = FixedNow.UtcDateTime }).ToList()
		}, new TelegramHistoryPage { Messages = [new TelegramMessage { Id = 100, Date = FixedNow.UtcDateTime }] });

		await CreateSut().ProcessChannelsAsync();

		tgMessages.Verify(x => x.SearchMessagesAsync(firstSessionId, It.IsAny<TelegramPeer>(), TelegramMessageFilter.Url,
			100, 101, 0, 0, "", It.IsAny<CancellationToken>(), true), Times.Once);
		storage.Verify(x => x.UpsertAsync(It.Is<DiscoveredPeerUpsert>(p => p.MarkAsCompleted && p.LastParsedId == 200),
			It.IsAny<CancellationToken>()), Times.Once);
	}

	[Theory]
	[InlineData(TelegramEntityType.Channel, true)]
	[InlineData(TelegramEntityType.Group, true)]
	[InlineData(TelegramEntityType.User, false)]
	[InlineData(TelegramEntityType.Bot, false)]
	public async Task ResolveHiddenLinks_AndKeepOnlyChannelsAndGroups(TelegramEntityType type, bool included)
	{
		SetupSuccessfulSource();
		SetupHistory(new TelegramHistoryPage { Messages = [new TelegramMessage
		{
			Id = 1, Date = FixedNow.UtcDateTime, Text = "Hidden link",
			Entities = [new TelegramMessageEntity(0, 6, TelegramMessageEntityType.TextUrl, "https://t.me/hiddenchat")]
		}] });
		publicLookup.Setup(x => x.LookupAsync("hiddenchat", It.IsAny<CancellationToken>()))
			.ReturnsAsync(TelegramOperationResult<TelegramPublicEntityInfo>.Success(new TelegramPublicEntityInfo
			{
				Type = type, Username = "hiddenchat", Title = "Hidden", MembersCount = (long)int.MaxValue + 1
			}));

		await CreateSut().ProcessChannelsAsync();

		storage.Verify(x => x.BulkUpsertAsync(It.Is<IReadOnlyCollection<DiscoveredPeerUpsert>>(items =>
			included ? items.Count == 1 && items.Single().ParticipantsCount == int.MaxValue : items.Count == 0),
			It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task MergeInviteIntoPublicPeer_AndIgnorePhoneNumberLinks()
	{
		SetupSuccessfulSource();
		SetupHistory(new TelegramHistoryPage
		{
			Chats = [Chat(200, "knownchat", "Known")],
			Messages = [new TelegramMessage { Id = 1, Date = FixedNow.UtcDateTime, Text = "https://t.me/+AbCd https://t.me/+79633081622" }]
		});
		publicLookup.Setup(x => x.LookupInviteAsync("AbCd", It.IsAny<CancellationToken>()))
			.ReturnsAsync(TelegramOperationResult<TelegramPublicEntityInfo>.Success(new TelegramPublicEntityInfo
			{
				Type = TelegramEntityType.Channel, Username = "knownchat", Title = "Known"
			}));

		await CreateSut().ProcessChannelsAsync();

		storage.Verify(x => x.BulkUpsertAsync(It.Is<IReadOnlyCollection<DiscoveredPeerUpsert>>(items =>
			items.Count == 1 && items.Single().Username == "knownchat" && items.Single().InviteHash == "AbCd"),
			It.IsAny<CancellationToken>()), Times.Once);
		publicLookup.Verify(x => x.LookupInviteAsync("79633081622", It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task KeepDistinctPeers_WhenTitlesMatch()
	{
		SetupSuccessfulSource();
		SetupHistory(new TelegramHistoryPage
		{
			Chats = [Chat(200, "knownchat", "Same"), Chat(300, null, "same")],
			Messages = [new TelegramMessage { Id = 1, Date = FixedNow.UtcDateTime, Text = "https://t.me/+AbCd" }]
		});
		publicLookup.Setup(x => x.LookupInviteAsync("AbCd", It.IsAny<CancellationToken>()))
			.ReturnsAsync(TelegramOperationResult<TelegramPublicEntityInfo>.Success(new TelegramPublicEntityInfo
			{
				Type = TelegramEntityType.Group, Title = "SAME"
			}));

		await CreateSut().ProcessChannelsAsync();

		storage.Verify(x => x.BulkUpsertAsync(It.Is<IReadOnlyCollection<DiscoveredPeerUpsert>>(items =>
			items.Count == 3 &&
			items.Any(p => p.TelegramId == 200 && p.Username == "knownchat") &&
			items.Any(p => p.TelegramId == 300 && p.Username == null) &&
			items.Any(p => p.InviteHash == "AbCd" && p.TelegramId == null)), It.IsAny<CancellationToken>()), Times.Once);
	}

	private void SetupSuccessfulSource() => SetupResolve(firstChannel,
		TelegramOperationResult<TelegramChatInfo>.Success(Chat(100, firstChannel.Username!, "Source")));

	private static TelegramChatInfo Chat(long id, string? username, string title) => new()
	{
		Id = id, AccessHash = 1, Username = username, Title = title, IsChannel = true, Peer = TelegramPeer.Channel(id, 1)
	};

	private void SetupHistory(params TelegramHistoryPage[] pages)
	{
		var sequence = tgMessages.SetupSequence(x => x.SearchMessagesAsync(firstSessionId, It.IsAny<TelegramPeer>(),
			TelegramMessageFilter.Url, 100, It.IsAny<int>(), It.IsAny<int>(), 0, "", It.IsAny<CancellationToken>(), true));
		foreach (var page in pages)
			sequence.ReturnsAsync(TelegramOperationResult<TelegramHistoryPage>.Success(page));
	}

	private void SetupQueue(params DiscoverChannelDto[] channels) =>
		storage
			.Setup(s => s.GetChannelsToProcessAsync(It.IsAny<int>(), It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync([..channels]);

	private void SetupResolve(DiscoverChannelDto channel, TelegramOperationResult<TelegramChatInfo> result) =>
		tgMessages
			.Setup(s => s.ResolveChannelAsync(
				It.IsAny<Guid>(), channel.Username!, It.IsAny<CancellationToken>(), It.IsAny<bool>()))
			.ReturnsAsync(result);

	private void SetupResolveThrows(DiscoverChannelDto channel, string message) =>
		tgMessages
			.Setup(s => s.ResolveChannelAsync(
				It.IsAny<Guid>(), channel.Username!, It.IsAny<CancellationToken>(), It.IsAny<bool>()))
			.ThrowsAsync(new InvalidOperationException(message));

	private void VerifyNeverMarkedAsError() =>
		storage.Verify(s => s.MarkAsErrorAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);

	private void SetupLastStartedAt(DateTimeOffset? value) =>
		statusStorage
			.Setup(s => s.GetLastStartedAtAsync(WorkerJobNames.DiscoverChannelLinks, It.IsAny<CancellationToken>()))
			.ReturnsAsync(value);

	private void VerifyNeverStarted() =>
		statusStorage.Verify(s => s.ReportStartedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

	private void VerifyQueueNeverRequested() =>
		storage.Verify(s => s.GetChannelsToProcessAsync(
			It.IsAny<int>(), It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);

	private DiscoverChannelLinksWorker CreateSut()
	{
		var provider = new Mock<IServiceProvider>();
		provider.Setup(p => p.GetService(typeof(ITelegramMessageService))).Returns(tgMessages.Object);
		provider.Setup(p => p.GetService(typeof(ITelegramPublicLookupService))).Returns(publicLookup.Object);

		var scope = new Mock<IServiceScope>();
		scope.Setup(s => s.ServiceProvider).Returns(provider.Object);

		var scopeFactory = new Mock<IServiceScopeFactory>();
		scopeFactory.Setup(f => f.CreateScope()).Returns(scope.Object);

		var lifetime = new Mock<IHostApplicationLifetime>();
		lifetime.Setup(l => l.ApplicationStopping).Returns(CancellationToken.None);

		return new DiscoverChannelLinksWorker(
			storage.Object,
			authService.Object,
			statusStorage.Object,
			scopeFactory.Object,
			new FixedTimeProvider(FixedNow),
			NullLogger<DiscoverChannelLinksWorker>.Instance,
			lifetime.Object);
	}

	private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
	{
		public override DateTimeOffset GetUtcNow() => now;

		public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
		{
			var timer = new Timer(callback, state, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
			if (dueTime != Timeout.InfiniteTimeSpan)
				timer.Change(TimeSpan.Zero, Timeout.InfiniteTimeSpan);
			return timer;
		}
	}
}
