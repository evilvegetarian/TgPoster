using Moq;
using Security.IdentityServices;
using Shared.Enums;
using Shouldly;
using TgPoster.API.Domain.UseCases.Repost.AddDestinationsFromDiscover;
using TgPoster.Exceptions.BadRequest;
using TgPoster.Exceptions.NotFound;
using TgPoster.Telegram.Abstractions;
using TgPoster.Telegram.Models;

namespace TgPoster.API.Domain.Tests.Repost;

public class AddDestinationsFromDiscoverUseCaseShould
{
	private const long SourceChannelId = 777;
	private readonly Mock<ITelegramChatService> chatService;
	private readonly Guid destinationId = Guid.NewGuid();
	private readonly Guid sessionId = Guid.NewGuid();
	private readonly Guid settingsId = Guid.NewGuid();
	private readonly Mock<IAddDestinationsFromDiscoverStorage> storage;
	private readonly AddDestinationsFromDiscoverUseCase sut;
	private readonly Guid userId = Guid.NewGuid();

	public AddDestinationsFromDiscoverUseCaseShould()
	{
		storage = new Mock<IAddDestinationsFromDiscoverStorage>();
		chatService = new Mock<ITelegramChatService>();
		var identityProvider = new Mock<IIdentityProvider>();
		identityProvider.Setup(x => x.Current).Returns(new Identity(userId));

		storage.Setup(s => s.GetSettingsDefaultsAsync(settingsId, userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new RepostSettingsDefaults(sessionId, SourceChannelId, 10, 60, 2, 30, 5));

		storage.Setup(s => s.GetExistingChatIdsAsync(settingsId, It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);

		storage.Setup(s => s.AddDestinationAsync(
				It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<string?>(), It.IsAny<string?>(),
				It.IsAny<int?>(), It.IsAny<ChatType>(), It.IsAny<Guid>(), It.IsAny<int>(),
				It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int?>(),
				It.IsAny<CancellationToken>()))
			.ReturnsAsync(destinationId);

		sut = new AddDestinationsFromDiscoverUseCase(storage.Object, chatService.Object, identityProvider.Object);
	}

	[Fact]
	public async Task ThrowInvalidRepostSettings_WhenNothingSelected()
	{
		var command = new AddDestinationsFromDiscoverCommand(settingsId, [], true);

		await Should.ThrowAsync<InvalidRepostSettingsException>(async () =>
			await sut.Handle(command, CancellationToken.None));
	}

	[Fact]
	public async Task ThrowInvalidRepostSettings_WhenBatchTooLarge()
	{
		var ids = Enumerable.Range(0, 21).Select(_ => Guid.NewGuid()).ToList();
		var command = new AddDestinationsFromDiscoverCommand(settingsId, ids, true);

		await Should.ThrowAsync<InvalidRepostSettingsException>(async () =>
			await sut.Handle(command, CancellationToken.None));
	}

	[Fact]
	public async Task ThrowRepostSettingsNotFound_WhenSettingsMissingOrForeign()
	{
		storage.Setup(s => s.GetSettingsDefaultsAsync(It.IsAny<Guid>(), userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync((RepostSettingsDefaults?)null);

		var command = new AddDestinationsFromDiscoverCommand(settingsId, [Guid.NewGuid()], true);

		await Should.ThrowAsync<RepostSettingsNotFoundException>(async () =>
			await sut.Handle(command, CancellationToken.None));
	}

	[Fact]
	public async Task AddChannelWithDefaultsFromSettings()
	{
		var candidate = SetupCandidate(username: "targetchan");
		SetupChat(candidate, 12345, true, true);

		var response = await Handle(candidate);

		response.AddedCount.ShouldBe(1);
		response.SkippedCount.ShouldBe(0);
		response.RateLimited.ShouldBeFalse();
		response.Results.Single().Outcome.ShouldBe(AddDestinationOutcome.Added);
		response.Results.Single().DestinationId.ShouldBe(destinationId);

		storage.Verify(s => s.AddDestinationAsync(
			settingsId, 12345, It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int?>(),
			ChatType.Channel, candidate.Id, 10, 60, 2, 30, 5, It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task SkipSourceChannel()
	{
		var candidate = SetupCandidate(telegramId: SourceChannelId);

		var response = await Handle(candidate);

		response.AddedCount.ShouldBe(0);
		response.Results.Single().Outcome.ShouldBe(AddDestinationOutcome.SourceChannel);
		VerifyNoDestinationAdded();
	}

	[Fact]
	public async Task SkipAlreadyAddedChannel()
	{
		var candidate = SetupCandidate(telegramId: 555);
		storage.Setup(s => s.GetExistingChatIdsAsync(settingsId, It.IsAny<CancellationToken>()))
			.ReturnsAsync([555]);

		var response = await Handle(candidate);

		response.Results.Single().Outcome.ShouldBe(AddDestinationOutcome.AlreadyAdded);
		VerifyNoDestinationAdded();
	}

	[Fact]
	public async Task SkipChannelWithoutWritePermission()
	{
		var candidate = SetupCandidate(username: "readonlychan");
		SetupChat(candidate, 4242, false, false);

		var response = await Handle(candidate);

		response.Results.Single().Outcome.ShouldBe(AddDestinationOutcome.NoWritePermission);
		VerifyNoDestinationAdded();
	}

	[Fact]
	public async Task SkipChannelWithoutMediaPermission()
	{
		var candidate = SetupCandidate(username: "nomediachan");
		SetupChat(candidate, 4243, true, false);

		var response = await Handle(candidate);

		response.Results.Single().Outcome.ShouldBe(AddDestinationOutcome.NoMediaPermission);
		VerifyNoDestinationAdded();
	}

	[Fact]
	public async Task RecordPermissionsInDiscover_EvenWhenChannelRejected()
	{
		var candidate = SetupCandidate(username: "readonlychan");
		SetupChat(candidate, 4242, true, false);

		await Handle(candidate);

		storage.Verify(s => s.UpdateDiscoveredChannelAsync(
			candidate.Id, 4242, It.IsAny<string?>(), It.IsAny<string?>(), ChatType.Channel,
			true, false, It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task ReportNotResolved_WhenChannelHasNoIdentifier()
	{
		var candidate = new DiscoverCandidate(Guid.NewGuid(), null, null, "Приватный канал", null, 100);
		SetupCandidates(candidate);

		var response = await Handle(candidate);

		response.Results.Single().Outcome.ShouldBe(AddDestinationOutcome.NotResolved);
		VerifyNoDestinationAdded();
	}

	[Fact]
	public async Task ReportNotResolved_WhenChannelMissingInDiscover()
	{
		var missingId = Guid.NewGuid();
		SetupCandidates();

		var response = await sut.Handle(
			new AddDestinationsFromDiscoverCommand(settingsId, [missingId], true),
			CancellationToken.None);

		response.Results.Single().DiscoveredChannelId.ShouldBe(missingId);
		response.Results.Single().Outcome.ShouldBe(AddDestinationOutcome.NotResolved);
	}

	[Fact]
	public async Task StopProcessing_WhenTelegramRateLimits()
	{
		var first = new DiscoverCandidate(Guid.NewGuid(), null, "floodchan", "Первый", null, 10);
		var second = new DiscoverCandidate(Guid.NewGuid(), null, "secondchan", "Второй", null, 20);
		SetupCandidates(first, second);

		chatService.Setup(c => c.TryGetChatInfoAsync(sessionId, "@floodchan", It.IsAny<bool>()))
			.ReturnsAsync(TelegramOperationResult<TelegramChatInfo>.Failed(
				TelegramOperationStatus.FloodWait, "FLOOD_WAIT_600", 600));

		var response = await sut.Handle(
			new AddDestinationsFromDiscoverCommand(settingsId, [first.Id, second.Id], true),
			CancellationToken.None);

		response.RateLimited.ShouldBeTrue();
		response.AddedCount.ShouldBe(0);
		response.Results.ShouldAllBe(x => x.Outcome == AddDestinationOutcome.RateLimited);
		chatService.Verify(c => c.TryGetChatInfoAsync(sessionId, "@secondchan", It.IsAny<bool>()), Times.Never);
	}

	[Fact]
	public async Task PassAutoJoinFlagToTelegram()
	{
		var candidate = SetupCandidate(username: "targetchan");
		SetupChat(candidate, 12345, true, true);

		await sut.Handle(
			new AddDestinationsFromDiscoverCommand(settingsId, [candidate.Id], false),
			CancellationToken.None);

		chatService.Verify(c => c.TryGetChatInfoAsync(sessionId, "@targetchan", false), Times.Once);
	}

	[Fact]
	public async Task ResolvePrivateChannelByInviteHash()
	{
		var candidate = new DiscoverCandidate(Guid.NewGuid(), null, null, "Приватный", "AbCdEf", 50);
		SetupCandidates(candidate);
		SetupChat(candidate, 9999, true, true, "https://t.me/+AbCdEf");

		var response = await Handle(candidate);

		response.AddedCount.ShouldBe(1);
		chatService.Verify(c => c.TryGetChatInfoAsync(sessionId, "https://t.me/+AbCdEf", It.IsAny<bool>()),
			Times.Once);
	}

	private Task<AddDestinationsFromDiscoverResponse> Handle(DiscoverCandidate candidate) =>
		sut.Handle(
			new AddDestinationsFromDiscoverCommand(settingsId, [candidate.Id], true),
			CancellationToken.None);

	private DiscoverCandidate SetupCandidate(string? username = null, long? telegramId = null)
	{
		var candidate = new DiscoverCandidate(
			Guid.NewGuid(),
			telegramId,
			username,
			"Тестовый канал",
			null,
			1000);
		SetupCandidates(candidate);
		return candidate;
	}

	private void SetupCandidates(params DiscoverCandidate[] candidates) =>
		storage.Setup(s => s.GetCandidatesAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(candidates.ToList());

	private void SetupChat(
		DiscoverCandidate candidate,
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
			Title = candidate.Title ?? "Канал",
			Username = candidate.Username,
			IsChannel = true,
			IsGroup = false,
			CanSendMessages = canSendMessages,
			CanSendMedia = canSendMedia,
			Peer = TelegramPeer.Channel(chatId, 1)
		};

		chatService.Setup(c => c.TryGetChatInfoAsync(
				sessionId,
				identifier ?? "@" + candidate.Username,
				It.IsAny<bool>()))
			.ReturnsAsync(TelegramOperationResult<TelegramChatInfo>.Success(info));
	}

	private void VerifyNoDestinationAdded() =>
		storage.Verify(s => s.AddDestinationAsync(
			It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<string?>(), It.IsAny<string?>(),
			It.IsAny<int?>(), It.IsAny<ChatType>(), It.IsAny<Guid>(), It.IsAny<int>(),
			It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int?>(),
			It.IsAny<CancellationToken>()), Times.Never);
}
