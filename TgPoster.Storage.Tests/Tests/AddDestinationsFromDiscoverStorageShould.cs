using Bogus;
using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.Enum;
using TgPoster.Storage.Storages.Repost;
using TgPoster.Storage.Tests.Builders;
using TgPoster.API.Domain.UseCases.Discover.ListDiscover;
using TgPoster.API.Domain.UseCases.Repost.AddDestinationsFromDiscover;
using SortDirection = TgPoster.API.Domain.UseCases.Discover.ListDiscover.SortDirection;

namespace TgPoster.Storage.Tests.Tests;

public sealed class AddDestinationsFromDiscoverStorageShould(StorageTestFixture fixture)
	: IClassFixture<StorageTestFixture>
{
	private static readonly Faker faker = FakerProvider.Instance;
	private readonly PosterContext context = fixture.GetDbContext();
	private readonly AddDestinationsFromDiscoverStorage sut = new(fixture.GetDbContext(), new GuidFactory());

	[Fact]
	public async Task GetSettingsDefaultsAsync_WithOwner_ShouldReturnDefaultsAndSourceChannel()
	{
		var user = await new UserBuilder(context).CreateAsync();
		var schedule = await new ScheduleBuilder(context).WithUserId(user.Id).CreateAsync();
		var session = await new TelegramSessionBuilder(context).CreateAsync();
		var settings = await new RepostSettingsBuilder(context)
			.WithScheduleId(schedule.Id)
			.WithTelegramSessionId(session.Id)
			.WithDefaultSettings(15, 120, 3, 40, 7)
			.CreateAsync();

		var result = await sut.GetSettingsDefaultsAsync(settings.Id, user.Id, CancellationToken.None);

		result.ShouldNotBeNull();
		result.TelegramSessionId.ShouldBe(session.Id);
		result.SourceChannelId.ShouldBe(schedule.ChannelId);
		result.SessionFloodWaitUntil.ShouldBeNull();
	}

	[Fact]
	public async Task GetSettingsDefaultsAsync_WithRestrictedSession_ShouldReturnFloodWait()
	{
		var user = await new UserBuilder(context).CreateAsync();
		var schedule = await new ScheduleBuilder(context).WithUserId(user.Id).CreateAsync();
		var session = await new TelegramSessionBuilder(context).CreateAsync();
		var settings = await new RepostSettingsBuilder(context)
			.WithScheduleId(schedule.Id)
			.WithTelegramSessionId(session.Id)
			.CreateAsync();

		var floodWaitUntil = DateTimeOffset.UtcNow.AddMinutes(30);
		var tracked = await context.TelegramSessions.FirstAsync(x => x.Id == session.Id);
		tracked.FloodWaitUntil = floodWaitUntil;
		await context.SaveChangesAsync();

		var result = await sut.GetSettingsDefaultsAsync(settings.Id, user.Id, CancellationToken.None);

		result.ShouldNotBeNull();
		result.SessionFloodWaitUntil.ShouldNotBeNull();
	}

	[Fact]
	public async Task GetSettingsDefaultsAsync_WithForeignUser_ShouldReturnNull()
	{
		var settings = await new RepostSettingsBuilder(context).CreateAsync();

		var result = await sut.GetSettingsDefaultsAsync(settings.Id, Guid.NewGuid(), CancellationToken.None);

		result.ShouldBeNull();
	}

	[Fact]
	public async Task GetCandidatesAsync_ShouldReturnOnlyRequestedChannels()
	{
		var requested = await CreateDiscoveredChannelAsync();
		var other = await CreateDiscoveredChannelAsync();

		var result = await sut.GetCandidatesAsync([requested.Id], CancellationToken.None);

		result.Count.ShouldBe(1);
		result[0].Id.ShouldBe(requested.Id);
		result[0].TelegramId.ShouldBe(requested.TelegramId);
		result[0].Username.ShouldBe(requested.Username);
		result[0].InviteHash.ShouldBe(requested.InviteHash);
		result.ShouldNotContain(x => x.Id == other.Id);
	}

	[Fact]
	public async Task GetCandidatesAsync_ShouldReturnKnownSendPermissions()
	{
		var channel = await CreateDiscoveredChannelAsync(canSendMessages: false, canSendMedia: true);

		var result = await sut.GetCandidatesAsync([channel.Id], CancellationToken.None);

		result.Single().CanSendMessages.ShouldBe(false);
		result.Single().CanSendMedia.ShouldBe(true);
	}

	[Fact]
	public async Task GetCandidatesAsync_ShouldSkipBannedChannels()
	{
		var banned = await CreateDiscoveredChannelAsync(isBanned: true);

		var result = await sut.GetCandidatesAsync([banned.Id], CancellationToken.None);

		result.ShouldBeEmpty();
	}

	[Fact]
	public async Task GetExistingChatIdsAsync_ShouldReturnChatIdsOfSettingsOnly()
	{
		var settings = await new RepostSettingsBuilder(context).CreateAsync();
		await new RepostDestinationBuilder(context)
			.WithRepostSettingsId(settings.Id)
			.WithChatIdentifier(4242)
			.CreateAsync();
		await new RepostDestinationBuilder(context)
			.WithChatIdentifier(9999)
			.CreateAsync();

		var result = await sut.GetExistingChatIdsAsync(settings.Id, CancellationToken.None);

		result.ShouldBe([4242]);
	}

	[Fact]
	public async Task CreateImportJobAsync_ShouldStoreJobWithAllItemsInOrder()
	{
		var settings = await new RepostSettingsBuilder(context).CreateAsync();
		var first = Guid.NewGuid();
		var second = Guid.NewGuid();

		var jobId = await sut.CreateImportJobAsync(
			settings.Id,
			false,
			[
				new ImportJobItemDto(first, "Первый", AddDestinationOutcome.Pending, null),
				new ImportJobItemDto(second, "Второй", AddDestinationOutcome.NoWritePermission, "Нельзя писать")
			],
			CancellationToken.None);

		var job = await context.RepostImportJobs
			.Include(x => x.Items)
			.AsNoTracking()
			.FirstAsync(x => x.Id == jobId);

		job.RepostSettingsId.ShouldBe(settings.Id);
		job.AutoJoin.ShouldBeFalse();
		job.Status.ShouldBe(RepostImportStatus.Pending);

		var items = job.Items.OrderBy(x => x.Order).ToList();
		items.Count.ShouldBe(2);
		items[0].DiscoveredChannelId.ShouldBe(first);
		items[0].Outcome.ShouldBe(AddDestinationOutcome.Pending);
		items[0].ProcessedAt.ShouldBeNull();
		items[1].DiscoveredChannelId.ShouldBe(second);
		items[1].Outcome.ShouldBe(AddDestinationOutcome.NoWritePermission);
		items[1].Error.ShouldBe("Нельзя писать");
		items[1].ProcessedAt.ShouldNotBeNull();
	}

	[Fact]
	public async Task GetCandidatesByFilterAsync_ShouldReturnOnlyChannelsMatchingFilter()
	{
		var category = NewCategory();
		var matching = await CreateDiscoveredChannelAsync(category: category, peerType: "channel");
		var otherType = await CreateDiscoveredChannelAsync(category: category, peerType: "chat");
		var otherCategory = await CreateDiscoveredChannelAsync(category: NewCategory(), peerType: "channel");

		var result = await sut.GetCandidatesByFilterAsync(
			FilterFor(category, peerType: "channel"), [], 100, CancellationToken.None);

		result.Select(x => x.Id).ShouldBe([matching.Id]);
		result.ShouldNotContain(x => x.Id == otherType.Id);
		result.ShouldNotContain(x => x.Id == otherCategory.Id);
	}

	[Fact]
	public async Task GetCandidatesByFilterAsync_ShouldSkipExcludedChatIds()
	{
		var category = NewCategory();
		var excluded = await CreateDiscoveredChannelAsync(category: category);
		var expected = await CreateDiscoveredChannelAsync(category: category);

		var result = await sut.GetCandidatesByFilterAsync(
			FilterFor(category), [excluded.TelegramId!.Value], 100, CancellationToken.None);

		result.Select(x => x.Id).ShouldBe([expected.Id]);
	}

	[Fact]
	public async Task GetCandidatesByFilterAsync_ShouldSkipChannelsWithKnownRestrictions()
	{
		var category = NewCategory();
		await CreateDiscoveredChannelAsync(category: category, canSendMessages: false);
		await CreateDiscoveredChannelAsync(category: category, canSendMedia: false);
		var expected = await CreateDiscoveredChannelAsync(category: category, canSendMessages: true);

		var result = await sut.GetCandidatesByFilterAsync(
			FilterFor(category), [], 100, CancellationToken.None);

		result.Select(x => x.Id).ShouldBe([expected.Id]);
	}

	[Fact]
	public async Task GetCandidatesByFilterAsync_ShouldSkipChannelsWithoutAnyIdentifier()
	{
		var category = NewCategory();
		var expected = await CreateDiscoveredChannelAsync(category: category);
		await CreateDiscoveredChannelAsync(category: category, withIdentifiers: false);

		var result = await sut.GetCandidatesByFilterAsync(
			FilterFor(category), [], 100, CancellationToken.None);

		result.Select(x => x.Id).ShouldBe([expected.Id]);
	}

	[Fact]
	public async Task GetCandidatesByFilterAsync_ShouldSkipBannedChannels()
	{
		var category = NewCategory();
		await CreateDiscoveredChannelAsync(category: category, isBanned: true);

		var result = await sut.GetCandidatesByFilterAsync(
			FilterFor(category), [], 100, CancellationToken.None);

		result.ShouldBeEmpty();
	}

	[Fact]
	public async Task GetCandidatesByFilterAsync_ShouldTakeMostPopularChannelsWithinLimit()
	{
		var category = NewCategory();
		await CreateDiscoveredChannelAsync(category: category, participantsCount: 100);
		var biggest = await CreateDiscoveredChannelAsync(category: category, participantsCount: 9000);
		var second = await CreateDiscoveredChannelAsync(category: category, participantsCount: 5000);

		var result = await sut.GetCandidatesByFilterAsync(
			FilterFor(category), [], 2, CancellationToken.None);

		result.Select(x => x.Id).ShouldBe([biggest.Id, second.Id]);
	}

	[Fact]
	public async Task GetCandidatesByFilterAsync_ShouldRespectParticipantsRange()
	{
		var category = NewCategory();
		await CreateDiscoveredChannelAsync(category: category, participantsCount: 100);
		var expected = await CreateDiscoveredChannelAsync(category: category, participantsCount: 5000);
		await CreateDiscoveredChannelAsync(category: category, participantsCount: 50000);

		var filter = FilterFor(category) with { MinParticipants = 1000, MaxParticipants = 10000 };

		var result = await sut.GetCandidatesByFilterAsync(filter, [], 100, CancellationToken.None);

		result.Select(x => x.Id).ShouldBe([expected.Id]);
	}

	[Fact]
	public async Task GetActiveJobIdAsync_ShouldReturnUnfinishedJob()
	{
		var settings = await new RepostSettingsBuilder(context).CreateAsync();
		var jobId = await CreateJobAsync(settings.Id, RepostImportStatus.CooldownWait);

		var result = await sut.GetActiveJobIdAsync(settings.Id, CancellationToken.None);

		result.ShouldBe(jobId);
	}

	[Fact]
	public async Task GetActiveJobIdAsync_ShouldReturnNull_WhenAllJobsFinished()
	{
		var settings = await new RepostSettingsBuilder(context).CreateAsync();
		await CreateJobAsync(settings.Id, RepostImportStatus.Completed);
		await CreateJobAsync(settings.Id, RepostImportStatus.Failed);

		var result = await sut.GetActiveJobIdAsync(settings.Id, CancellationToken.None);

		result.ShouldBeNull();
	}

	[Fact]
	public async Task GetActiveJobIdAsync_ShouldIgnoreJobsOfOtherSettings()
	{
		var settings = await new RepostSettingsBuilder(context).CreateAsync();
		var other = await new RepostSettingsBuilder(context).CreateAsync();
		await CreateJobAsync(other.Id, RepostImportStatus.InProgress);

		var result = await sut.GetActiveJobIdAsync(settings.Id, CancellationToken.None);

		result.ShouldBeNull();
	}

	private static string NewCategory() => "Категория-" + Guid.NewGuid();

	private static DiscoverImportFilter FilterFor(string category, string? peerType = null) =>
		new(category, null, peerType, null, null, DiscoverSortBy.Participants, SortDirection.Desc);

	private async Task<Guid> CreateJobAsync(Guid repostSettingsId, RepostImportStatus status)
	{
		var job = new RepostImportJob
		{
			Id = Guid.NewGuid(),
			RepostSettingsId = repostSettingsId,
			AutoJoin = true,
			Status = status
		};

		await context.RepostImportJobs.AddAsync(job);
		await context.SaveChangesAsync();

		return job.Id;
	}

	private async Task<DiscoveredChannel> CreateDiscoveredChannelAsync(
		bool isBanned = false,
		bool? canSendMessages = null,
		bool? canSendMedia = null,
		string? category = null,
		string? peerType = null,
		int? participantsCount = null,
		bool withIdentifiers = true
	)
	{
		var channel = new DiscoveredChannel
		{
			Id = Guid.NewGuid(),
			TelegramId = withIdentifiers ? faker.Random.Long(1_000_000_000, 9_000_000_000) : null,
			Username = withIdentifiers ? "discovered" + faker.Random.Number(10000, 99999) : null,
			Title = faker.Company.CompanyName(),
			InviteHash = withIdentifiers ? faker.Random.AlphaNumeric(16) : null,
			ParticipantsCount = participantsCount ?? faker.Random.Number(100, 10000),
			PeerType = peerType,
			Category = category,
			Status = DiscoveryStatus.Completed,
			CanSendMessages = canSendMessages,
			CanSendMedia = canSendMedia,
			IsBanned = isBanned
		};

		await context.DiscoveredChannels.AddAsync(channel);
		await context.SaveChangesAsync();

		return channel;
	}
}
