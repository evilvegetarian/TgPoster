using MassTransit;
using Moq;
using Security.IdentityServices;
using Shared.Enums;
using Shared.Telegram;
using Shouldly;
using TgPoster.API.Domain.UseCases.Discover.ListDiscover;
using TgPoster.API.Domain.UseCases.Repost.AddDestinationsFromDiscover;
using SortDirection = TgPoster.API.Domain.UseCases.Discover.ListDiscover.SortDirection;
using TgPoster.API.Domain.UseCases.Repost.GetRepostImportJob;
using TgPoster.Exceptions.BadRequest;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.Tests.Repost;

public class AddDestinationsFromDiscoverUseCaseShould
{
	private const long SourceChannelId = 777;

	private static readonly DiscoverImportFilter Filter = new(
		"Технологии",
		null,
		"channel",
		1000,
		null,
		DiscoverSortBy.Participants,
		SortDirection.Desc);

	private readonly Mock<IBus> bus;
	private readonly Guid jobId = Guid.NewGuid();
	private readonly Guid sessionId = Guid.NewGuid();
	private readonly Guid settingsId = Guid.NewGuid();
	private readonly Mock<IAddDestinationsFromDiscoverStorage> storage;
	private readonly AddDestinationsFromDiscoverUseCase sut;
	private readonly Guid userId = Guid.NewGuid();

	public AddDestinationsFromDiscoverUseCaseShould()
	{
		storage = new Mock<IAddDestinationsFromDiscoverStorage>();
		bus = new Mock<IBus>();
		var identityProvider = new Mock<IIdentityProvider>();
		identityProvider.Setup(x => x.Current).Returns(new Identity(userId));

		storage.Setup(s => s.GetSettingsDefaultsAsync(settingsId, userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new RepostSettingsDefaults(sessionId, SourceChannelId, null));

		storage.Setup(s => s.GetExistingChatIdsAsync(settingsId, It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);

		storage.Setup(s => s.CreateImportJobAsync(
				It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<IReadOnlyList<ImportJobItemDto>>(),
				It.IsAny<CancellationToken>()))
			.ReturnsAsync(jobId);

		sut = new AddDestinationsFromDiscoverUseCase(storage.Object, bus.Object, identityProvider.Object);
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
	public async Task QueueChannelForBackgroundProcessing()
	{
		var candidate = SetupCandidate(username: "targetchan");

		var response = await Handle(candidate);

		response.JobId.ShouldBe(jobId);
		response.Status.ShouldBe(RepostImportStatus.Pending);
		response.TotalCount.ShouldBe(1);
		response.PendingCount.ShouldBe(1);
		response.SkippedCount.ShouldBe(0);
		response.AddedCount.ShouldBe(0);
		response.Results.Single().Outcome.ShouldBe(AddDestinationOutcome.Pending);

		bus.Verify(b => b.Publish(
			It.Is<ImportRepostDestinationsContract>(c => c.JobId == jobId),
			It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task SkipChannelBannedFromWriting_WithoutQueueing()
	{
		var candidate = SetupCandidate(username: "readonlychan", canSendMessages: false);

		var response = await Handle(candidate);

		response.PendingCount.ShouldBe(0);
		response.SkippedCount.ShouldBe(1);
		response.Status.ShouldBe(RepostImportStatus.Completed);
		response.Results.Single().Outcome.ShouldBe(AddDestinationOutcome.NoWritePermission);
		VerifyNothingPublished();
	}

	[Fact]
	public async Task SkipChannelBannedFromMedia_WithoutQueueing()
	{
		var candidate = SetupCandidate(username: "nomediachan", canSendMedia: false);

		var response = await Handle(candidate);

		response.PendingCount.ShouldBe(0);
		response.Results.Single().Outcome.ShouldBe(AddDestinationOutcome.NoMediaPermission);
		VerifyNothingPublished();
	}

	[Fact]
	public async Task QueueChannel_WhenPermissionsWereNeverChecked()
	{
		var candidate = SetupCandidate(username: "unknownchan");

		var response = await Handle(candidate);

		response.Results.Single().Outcome.ShouldBe(AddDestinationOutcome.Pending);
	}

	[Fact]
	public async Task SkipSourceChannel()
	{
		var candidate = SetupCandidate(telegramId: SourceChannelId);

		var response = await Handle(candidate);

		response.PendingCount.ShouldBe(0);
		response.Results.Single().Outcome.ShouldBe(AddDestinationOutcome.SourceChannel);
		VerifyNothingPublished();
	}

	[Fact]
	public async Task SkipAlreadyAddedChannel()
	{
		var candidate = SetupCandidate(telegramId: 555);
		storage.Setup(s => s.GetExistingChatIdsAsync(settingsId, It.IsAny<CancellationToken>()))
			.ReturnsAsync([555]);

		var response = await Handle(candidate);

		response.Results.Single().Outcome.ShouldBe(AddDestinationOutcome.AlreadyAdded);
		VerifyNothingPublished();
	}

	[Fact]
	public async Task ReportNotResolved_WhenChannelHasNoIdentifier()
	{
		var candidate = new DiscoverCandidate(Guid.NewGuid(), null, null, "Приватный канал", null, null, null);
		SetupCandidates(candidate);

		var response = await Handle(candidate);

		response.Results.Single().Outcome.ShouldBe(AddDestinationOutcome.NotResolved);
		VerifyNothingPublished();
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
	public async Task QueueOnlyChannelsThatNeedTelegram()
	{
		var writable = new DiscoverCandidate(Guid.NewGuid(), null, "goodchan", "Хороший", null, null, null);
		var restricted = new DiscoverCandidate(Guid.NewGuid(), null, "badchan", "Плохой", null, false, null);
		SetupCandidates(writable, restricted);

		var response = await sut.Handle(
			new AddDestinationsFromDiscoverCommand(settingsId, [writable.Id, restricted.Id], true),
			CancellationToken.None);

		response.TotalCount.ShouldBe(2);
		response.PendingCount.ShouldBe(1);
		response.SkippedCount.ShouldBe(1);
		response.Results.First(x => x.DiscoveredChannelId == writable.Id).Outcome
			.ShouldBe(AddDestinationOutcome.Pending);
		response.Results.First(x => x.DiscoveredChannelId == restricted.Id).Outcome
			.ShouldBe(AddDestinationOutcome.NoWritePermission);
	}

	[Fact]
	public async Task ReportRetryAfter_WhenSessionIsUnderFloodWait()
	{
		storage.Setup(s => s.GetSettingsDefaultsAsync(settingsId, userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new RepostSettingsDefaults(sessionId, SourceChannelId,
				DateTimeOffset.UtcNow.AddMinutes(10)));
		var candidate = SetupCandidate(username: "targetchan");

		var response = await Handle(candidate);

		response.RetryAfterSeconds.ShouldNotBeNull();
		response.RetryAfterSeconds!.Value.ShouldBeGreaterThan(0);
	}

	[Fact]
	public async Task PassAutoJoinFlagToJob()
	{
		var candidate = SetupCandidate(username: "targetchan");

		await sut.Handle(
			new AddDestinationsFromDiscoverCommand(settingsId, [candidate.Id], false),
			CancellationToken.None);

		storage.Verify(s => s.CreateImportJobAsync(
			settingsId, false, It.IsAny<IReadOnlyList<ImportJobItemDto>>(), It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task DeduplicateRequestedChannels()
	{
		var candidate = SetupCandidate(username: "targetchan");

		var response = await sut.Handle(
			new AddDestinationsFromDiscoverCommand(settingsId, [candidate.Id, candidate.Id], true),
			CancellationToken.None);

		response.TotalCount.ShouldBe(1);
	}

	[Fact]
	public async Task ThrowInvalidRepostSettings_WhenSettingsAlreadyHaveRunningJob()
	{
		storage.Setup(s => s.GetActiveJobIdAsync(settingsId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Guid.NewGuid());
		var candidate = SetupCandidate(username: "targetchan");

		await Should.ThrowAsync<InvalidRepostSettingsException>(async () =>
			await Handle(candidate));

		VerifyNothingPublished();
	}

	[Fact]
	public async Task QueueChannelsMatchingFilter()
	{
		var first = new DiscoverCandidate(Guid.NewGuid(), null, "firstchan", "Первый", null, null, null);
		var second = new DiscoverCandidate(Guid.NewGuid(), null, "secondchan", "Второй", null, null, null);
		SetupFilterCandidates(first, second);

		var response = await HandleFilter();

		response.TotalCount.ShouldBe(2);
		response.PendingCount.ShouldBe(2);
		response.Status.ShouldBe(RepostImportStatus.Pending);
		response.Results.Select(x => x.DiscoveredChannelId).ShouldBe([first.Id, second.Id]);

		bus.Verify(b => b.Publish(
			It.Is<ImportRepostDestinationsContract>(c => c.JobId == jobId),
			It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task ExcludeSourceChannelAndExistingDestinations_WhenSelectingByFilter()
	{
		storage.Setup(s => s.GetExistingChatIdsAsync(settingsId, It.IsAny<CancellationToken>()))
			.ReturnsAsync([555]);
		SetupFilterCandidates(new DiscoverCandidate(Guid.NewGuid(), null, "chan", "Канал", null, null, null));

		await HandleFilter();

		storage.Verify(s => s.GetCandidatesByFilterAsync(
			It.IsAny<DiscoverImportFilter>(),
			It.Is<IReadOnlyCollection<long>>(x => x.Contains(555L) && x.Contains(SourceChannelId)),
			200,
			It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task ThrowInvalidRepostSettings_WhenFilterMatchesNothing()
	{
		SetupFilterCandidates();

		await Should.ThrowAsync<InvalidRepostSettingsException>(async () => await HandleFilter());

		VerifyNothingPublished();
	}

	[Fact]
	public async Task SkipRestrictedChannel_WhenSelectingByFilter()
	{
		var restricted = new DiscoverCandidate(Guid.NewGuid(), null, "badchan", "Плохой", null, false, null);
		SetupFilterCandidates(restricted);

		var response = await HandleFilter();

		response.PendingCount.ShouldBe(0);
		response.SkippedCount.ShouldBe(1);
		response.Results.Single().Outcome.ShouldBe(AddDestinationOutcome.NoWritePermission);
		VerifyNothingPublished();
	}

	private Task<RepostImportJobResponse> HandleFilter() =>
		sut.Handle(
			new AddDestinationsFromDiscoverCommand(settingsId, [], true, Filter),
			CancellationToken.None);

	private void SetupFilterCandidates(params DiscoverCandidate[] candidates) =>
		storage.Setup(s => s.GetCandidatesByFilterAsync(
				It.IsAny<DiscoverImportFilter>(),
				It.IsAny<IReadOnlyCollection<long>>(),
				It.IsAny<int>(),
				It.IsAny<CancellationToken>()))
			.ReturnsAsync(candidates.ToList());

	private Task<RepostImportJobResponse> Handle(DiscoverCandidate candidate) =>
		sut.Handle(
			new AddDestinationsFromDiscoverCommand(settingsId, [candidate.Id], true),
			CancellationToken.None);

	private DiscoverCandidate SetupCandidate(
		string? username = null,
		long? telegramId = null,
		bool? canSendMessages = null,
		bool? canSendMedia = null
	)
	{
		var candidate = new DiscoverCandidate(
			Guid.NewGuid(),
			telegramId,
			username,
			"Тестовый канал",
			null,
			canSendMessages,
			canSendMedia);
		SetupCandidates(candidate);

		return candidate;
	}

	private void SetupCandidates(params DiscoverCandidate[] candidates) =>
		storage.Setup(s => s.GetCandidatesAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(candidates.ToList());

	private void VerifyNothingPublished() =>
		bus.Verify(b => b.Publish(
			It.IsAny<ImportRepostDestinationsContract>(),
			It.IsAny<CancellationToken>()), Times.Never);
}
