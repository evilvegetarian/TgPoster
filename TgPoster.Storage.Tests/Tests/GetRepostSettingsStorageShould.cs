using Shared.Enums;
using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Storages.Repost;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public class GetRepostSettingsStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context = fixture.GetDbContext();
	private readonly CancellationToken ct = CancellationToken.None;
	private readonly GetRepostSettingsStorage sut = new(fixture.GetDbContext());

	[Fact]
	public async Task GetAsync_ShouldShowBannedFromAccountRegistry_AndMembershipOtherwise()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync(ct);
		var session = await new TelegramSessionBuilder(context).WithUserId(schedule.UserId).CreateAsync(ct);
		var settings = await new RepostSettingsBuilder(context)
			.WithSchedule(schedule)
			.WithTelegramSession(session)
			.CreateAsync(ct);
		var banned = await new RepostDestinationBuilder(context)
			.WithRepostSettingsId(settings.Id)
			.WithIsActive(false)
			.WithChatStatus(ChatStatus.Active)
			.CreateAsync(ct);
		var left = await new RepostDestinationBuilder(context)
			.WithRepostSettingsId(settings.Id)
			.WithChatStatus(ChatStatus.Left)
			.CreateAsync(ct);
		await new TelegramSessionChatBanBuilder(context)
			.WithTelegramSessionId(session.Id)
			.WithChatId(banned.ChatId)
			.CreateAsync(ct);
		// Бан другого аккаунта в том же чате к этим настройкам не относится
		await new TelegramSessionChatBanBuilder(context).WithChatId(left.ChatId).CreateAsync(ct);

		var response = await sut.GetAsync(settings.Id, schedule.UserId, ct);

		response.ShouldNotBeNull();
		response.Destinations.Single(x => x.Id == banned.Id).ChatStatus.ShouldBe(ChatStatus.Banned);
		response.Destinations.Single(x => x.Id == left.Id).ChatStatus.ShouldBe(ChatStatus.Left);
	}

	[Fact]
	public async Task GetAsync_WithExistingIdAndUserId_ShouldReturnRepostSettings()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync(ct);
		var telegramSession = await new TelegramSessionBuilder(context)
			.WithUserId(schedule.UserId)
			.CreateAsync(ct);
		var repostSettings = await new RepostSettingsBuilder(context)
			.WithSchedule(schedule)
			.WithTelegramSession(telegramSession)
			.CreateAsync(ct);

		var response = await sut.GetAsync(repostSettings.Id, schedule.UserId, ct);

		response.ShouldNotBeNull();
		response.Id.ShouldBe(repostSettings.Id);
		response.ScheduleId.ShouldBe(schedule.Id);
		response.ScheduleName.ShouldBe(schedule.Name);
		response.TelegramSessionId.ShouldBe(telegramSession.Id);
		response.TelegramSessionName.ShouldBe(telegramSession.Name);
		response.IsActive.ShouldBe(repostSettings.IsActive);
		response.Destinations.ShouldBeEmpty();
	}

	[Fact]
	public async Task GetAsync_WithDestinations_ShouldReturnDestinationsList()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync(ct);
		var telegramSession = await new TelegramSessionBuilder(context)
			.WithUserId(schedule.UserId)
			.CreateAsync(ct);
		var repostSettings = await new RepostSettingsBuilder(context)
			.WithSchedule(schedule)
			.WithTelegramSession(telegramSession)
			.CreateAsync(ct);

		var destination1 = await new RepostDestinationBuilder(context)
			.WithRepostSettings(repostSettings)
			.WithChatIdentifier(-1001234567890)
			.WithIsActive(true)
			.CreateAsync(ct);

		var destination2 = await new RepostDestinationBuilder(context)
			.WithRepostSettings(repostSettings)
			.WithChatIdentifier(-1009876543210)
			.WithIsActive(false)
			.CreateAsync(ct);

		var response = await sut.GetAsync(repostSettings.Id, schedule.UserId, ct);

		response.ShouldNotBeNull();
		response.Destinations.Count.ShouldBe(2);
		response.Destinations.ShouldContain(d => d.Id == destination1.Id && d.ChatId == -1001234567890 && d.IsActive);
		response.Destinations.ShouldContain(d => d.Id == destination2.Id && d.ChatId == -1009876543210 && !d.IsActive);
	}

	[Fact]
	public async Task GetAsync_WithNonExistingId_ShouldReturnNull()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync(ct);
		await new RepostSettingsBuilder(context)
			.WithSchedule(schedule)
			.CreateAsync(ct);

		var response = await sut.GetAsync(Guid.NewGuid(), schedule.UserId, ct);

		response.ShouldBeNull();
	}

	[Fact]
	public async Task GetAsync_WithDifferentUserId_ShouldReturnNull()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync(ct);
		var repostSettings = await new RepostSettingsBuilder(context)
			.WithSchedule(schedule)
			.CreateAsync(ct);

		var response = await sut.GetAsync(repostSettings.Id, Guid.NewGuid(), ct);

		response.ShouldBeNull();
	}

	[Fact]
	public async Task GetAsync_WithNonExistingIdAndUserId_ShouldReturnNull()
	{
		var response = await sut.GetAsync(Guid.NewGuid(), Guid.NewGuid(), ct);

		response.ShouldBeNull();
	}

	[Fact]
	public async Task GetAsync_ShouldReturnLastSuccessfulRepostAndTodayCount()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync(ct);
		var repostSettings = await new RepostSettingsBuilder(context)
			.WithSchedule(schedule)
			.CreateAsync(ct);
		var destination = await new RepostDestinationBuilder(context)
			.WithRepostSettings(repostSettings)
			.CreateAsync(ct);
		var silentDestination = await new RepostDestinationBuilder(context)
			.WithRepostSettings(repostSettings)
			.CreateAsync(ct);
		var message = await new MessageBuilder(context).WithScheduleId(schedule.Id).CreateAsync();

		var todayUtc = DateTime.UtcNow.Date;
		var lastRepostAt = todayUtc.AddMinutes(2);
		AddLog(message.Id, destination.Id, RepostStatus.Success, todayUtc.AddHours(-1));
		AddLog(message.Id, destination.Id, RepostStatus.Success, todayUtc.AddMinutes(1));
		AddLog(message.Id, destination.Id, RepostStatus.Success, lastRepostAt);
		AddLog(message.Id, destination.Id, RepostStatus.Skipped, null);
		AddLog(message.Id, destination.Id, RepostStatus.Failed, null);
		await context.SaveChangesAsync(ct);

		var response = await sut.GetAsync(repostSettings.Id, schedule.UserId, ct);

		response.ShouldNotBeNull();
		var withLogs = response.Destinations.Single(d => d.Id == destination.Id);
		withLogs.LastRepostAt.ShouldNotBeNull();
		withLogs.LastRepostAt.Value.ShouldBe(lastRepostAt, TimeSpan.FromSeconds(1));
		withLogs.RepostsToday.ShouldBe(2);

		var withoutLogs = response.Destinations.Single(d => d.Id == silentDestination.Id);
		withoutLogs.LastRepostAt.ShouldBeNull();
		withoutLogs.RepostsToday.ShouldBe(0);
	}

	[Fact]
	public async Task GetAsync_ShouldReturnBanErrorOfAccount()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync(ct);
		var session = await new TelegramSessionBuilder(context).WithUserId(schedule.UserId).CreateAsync(ct);
		var settings = await new RepostSettingsBuilder(context)
			.WithSchedule(schedule)
			.WithTelegramSession(session)
			.CreateAsync(ct);
		var banned = await new RepostDestinationBuilder(context).WithRepostSettingsId(settings.Id).CreateAsync(ct);
		var healthy = await new RepostDestinationBuilder(context).WithRepostSettingsId(settings.Id).CreateAsync(ct);
		await new TelegramSessionChatBanBuilder(context)
			.WithTelegramSessionId(session.Id)
			.WithChatId(banned.ChatId)
			.CreateAsync(ct);

		var response = await sut.GetAsync(settings.Id, schedule.UserId, ct);

		response.ShouldNotBeNull();
		var bannedDto = response.Destinations.Single(x => x.Id == banned.Id);
		bannedDto.BanError.ShouldBe("CHANNEL_PRIVATE");
		bannedDto.BannedAt.ShouldNotBeNull();
		response.Destinations.Single(x => x.Id == healthy.Id).BanError.ShouldBeNull();
	}

	[Fact]
	public async Task GetAsync_ShouldReturnRemovedDestinations_ExceptPurgedAndReaddedChats()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync(ct);
		var settings = await new RepostSettingsBuilder(context).WithSchedule(schedule).CreateAsync(ct);
		var otherSettings = await new RepostSettingsBuilder(context).CreateAsync(ct);
		await CreateRemovedAsync(settings.Id, 101, DateTimeOffset.UtcNow.AddDays(-2));
		var removedLast = await CreateRemovedAsync(settings.Id, 101, DateTimeOffset.UtcNow.AddDays(-1));
		await CreateRemovedAsync(settings.Id, 102, DateTimeOffset.UtcNow.AddDays(-1), purged: true);
		await CreateRemovedAsync(settings.Id, 103, DateTimeOffset.UtcNow.AddDays(-1));
		var readded = await new RepostDestinationBuilder(context)
			.WithRepostSettingsId(settings.Id)
			.WithChatIdentifier(103)
			.CreateAsync(ct);
		await CreateRemovedAsync(otherSettings.Id, 104, DateTimeOffset.UtcNow);

		var response = await sut.GetAsync(settings.Id, schedule.UserId, ct);

		response.ShouldNotBeNull();
		response.Destinations.Select(x => x.Id).ShouldBe([readded.Id]);
		var single = response.RemovedDestinations.ShouldHaveSingleItem();
		single.Id.ShouldBe(removedLast.Id);
		single.DeletedAt.ShouldNotBeNull();
		single.IsActive.ShouldBeFalse();
	}

	private async Task<RepostDestination> CreateRemovedAsync(
		Guid settingsId,
		long chatId,
		DateTimeOffset deletedAt,
		bool purged = false
	)
	{
		var destination = new RepostDestinationBuilder(context)
			.WithRepostSettingsId(settingsId)
			.WithChatIdentifier(chatId)
			.Build();
		destination.Deleted = deletedAt;
		destination.PurgedAt = purged ? deletedAt : null;
		context.Add(destination);
		await context.SaveChangesAsync(ct);
		return destination;
	}

	private void AddLog(Guid messageId, Guid destinationId, RepostStatus status, DateTime? repostedAt) =>
		context.Add(new RepostLog
		{
			Id = Guid.NewGuid(),
			MessageId = messageId,
			RepostDestinationId = destinationId,
			Status = status,
			Reason = status == RepostStatus.Success ? RepostLogReason.None : RepostLogReason.SkipProbability,
			RepostedAt = repostedAt
		});
}