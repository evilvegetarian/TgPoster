using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Storages.Repost;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public sealed class SetRepostDestinationsActiveStorageShould : IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context;
	private readonly CancellationToken ct = CancellationToken.None;
	private readonly SetRepostDestinationsActiveStorage sut;

	public SetRepostDestinationsActiveStorageShould(StorageTestFixture fixture)
	{
		context = fixture.GetDbContext();
		sut = new SetRepostDestinationsActiveStorage(context, new GuidFactory());
	}

	[Fact]
	public async Task GetTelegramSessionIdAsync_ForOwner_ShouldReturnSession()
	{
		var (settings, userId) = await CreateSettingsAsync();

		var result = await sut.GetTelegramSessionIdAsync(settings.Id, userId, ct);

		result.ShouldBe(settings.TelegramSessionId);
	}

	[Fact]
	public async Task GetTelegramSessionIdAsync_ForAnotherUser_ShouldReturnNull()
	{
		var (settings, _) = await CreateSettingsAsync();

		var result = await sut.GetTelegramSessionIdAsync(settings.Id, Guid.NewGuid(), ct);

		result.ShouldBeNull();
	}

	[Fact]
	public async Task GetDestinationsAsync_ShouldReturnOnlyDestinationsOfSettings()
	{
		var (settings, _) = await CreateSettingsAsync();
		var (otherSettings, _) = await CreateSettingsAsync();
		var own = await CreateDestinationAsync(settings.Id, false, ChatStatus.Left);
		var foreign = await CreateDestinationAsync(otherSettings.Id, true, ChatStatus.Active);

		var result = await sut.GetDestinationsAsync(settings.Id, [own.Id, foreign.Id], ct);

		var state = result.ShouldHaveSingleItem();
		state.Id.ShouldBe(own.Id);
		state.ChatId.ShouldBe(own.ChatId);
		state.IsActive.ShouldBeFalse();
		state.ChatStatus.ShouldBe(ChatStatus.Left);
	}

	[Fact]
	public async Task ActivateAsync_ShouldEnableAndMarkChatActive()
	{
		var (settings, _) = await CreateSettingsAsync();
		var left = await CreateDestinationAsync(settings.Id, false, ChatStatus.Left);
		var untouched = await CreateDestinationAsync(settings.Id, false, ChatStatus.Banned);

		await sut.ActivateAsync([left.Id], ct);

		var activated = await LoadAsync(left.Id);
		activated.IsActive.ShouldBeTrue();
		activated.ChatStatus.ShouldBe(ChatStatus.Active);
		var other = await LoadAsync(untouched.Id);
		other.IsActive.ShouldBeFalse();
		other.ChatStatus.ShouldBe(ChatStatus.Banned);
	}

	[Fact]
	public async Task DeactivateAsync_ShouldDisableAndKeepChatStatus()
	{
		var (settings, _) = await CreateSettingsAsync();
		var destination = await CreateDestinationAsync(settings.Id, true, ChatStatus.Left);

		await sut.DeactivateAsync([destination.Id], ct);

		var deactivated = await LoadAsync(destination.Id);
		deactivated.IsActive.ShouldBeFalse();
		deactivated.ChatStatus.ShouldBe(ChatStatus.Left);
	}

	[Fact]
	public async Task GetBannedChatIdsAsync_ShouldReturnOnlyActiveBansOfSession()
	{
		var banned = await new TelegramSessionChatBanBuilder(context).CreateAsync(ct);
		var sessionId = banned.TelegramSessionId;
		var removed = await new TelegramSessionChatBanBuilder(context)
			.WithTelegramSessionId(sessionId)
			.WithDeleted(DateTimeOffset.UtcNow)
			.CreateAsync(ct);
		var otherSession = await new TelegramSessionChatBanBuilder(context).CreateAsync(ct);
		var notRequested = await new TelegramSessionChatBanBuilder(context)
			.WithTelegramSessionId(sessionId)
			.CreateAsync(ct);

		var result = await sut.GetBannedChatIdsAsync(
			sessionId,
			[banned.ChatId, removed.ChatId, otherSession.ChatId, 42],
			ct);

		result.ShouldBe([banned.ChatId]);
		result.ShouldNotContain(notRequested.ChatId);
	}

	[Fact]
	public async Task AddAccountBansAsync_ShouldRecordEveryChatOnce()
	{
		var session = await new TelegramSessionBuilder(context).CreateAsync(ct);
		await new TelegramSessionChatBanBuilder(context)
			.WithTelegramSessionId(session.Id)
			.WithChatId(111)
			.CreateAsync(ct);

		await sut.AddAccountBansAsync(
			session.Id,
			new Dictionary<long, string> { [111] = "CHANNEL_FORBIDDEN", [222] = "ограничен" },
			ct);

		var bans = await context.TelegramSessionChatBans
			.AsNoTracking()
			.Where(x => x.TelegramSessionId == session.Id)
			.ToListAsync(ct);
		bans.Count.ShouldBe(2);
		bans.Single(x => x.ChatId == 111).Error.ShouldBe("CHANNEL_FORBIDDEN");
		bans.Single(x => x.ChatId == 222).Error.ShouldBe("ограничен");
	}

	private async Task<(RepostSettings Settings, Guid UserId)> CreateSettingsAsync()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync(ct);
		var settings = await new RepostSettingsBuilder(context)
			.WithSchedule(schedule)
			.CreateAsync(ct);

		return (settings, schedule.UserId);
	}

	private Task<RepostDestination> CreateDestinationAsync(Guid settingsId, bool isActive, ChatStatus chatStatus) =>
		new RepostDestinationBuilder(context)
			.WithRepostSettingsId(settingsId)
			.WithIsActive(isActive)
			.WithChatStatus(chatStatus)
			.CreateAsync(ct);

	private Task<RepostDestination> LoadAsync(Guid id) =>
		context.Set<RepostDestination>().AsNoTracking().FirstAsync(x => x.Id == id, ct);
}
