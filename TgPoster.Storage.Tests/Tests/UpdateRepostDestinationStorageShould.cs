using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Storages.Repost;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public sealed class UpdateRepostDestinationStorageShould : IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context;
	private readonly UpdateRepostDestinationStorage sut;

	public UpdateRepostDestinationStorageShould(StorageTestFixture fixture)
	{
		context = fixture.GetDbContext();
		sut = new UpdateRepostDestinationStorage(context, new GuidFactory());
	}

	[Fact]
	public async Task GetDestinationAsync_WithExistingDestination_ShouldReturnSessionChatAndStatus()
	{
		var destination = await new RepostDestinationBuilder(context)
			.WithChatIdentifier(1375846471)
			.WithUsername("wc_world")
			.WithIsActive(false)
			.WithChatStatus(ChatStatus.Left)
			.CreateAsync();
		var sessionId = await context.RepostSettings
			.Where(x => x.Id == destination.RepostSettingsId)
			.Select(x => x.TelegramSessionId)
			.FirstAsync();

		var result = await sut.GetDestinationAsync(destination.Id, CancellationToken.None);

		result.ShouldNotBeNull();
		result.TelegramSessionId.ShouldBe(sessionId);
		result.ChatId.ShouldBe(1375846471);
		result.Username.ShouldBe("wc_world");
		result.IsActive.ShouldBeFalse();
		result.ChatStatus.ShouldBe(ChatStatus.Left);
	}

	[Fact]
	public async Task GetDestinationAsync_WithNonExistingDestination_ShouldReturnNull()
	{
		var result = await sut.GetDestinationAsync(Guid.NewGuid(), CancellationToken.None);

		result.ShouldBeNull();
	}

	[Fact]
	public async Task UpdateDestinationAsync_WithValidData_ShouldUpdateIsActive()
	{
		var destination = await new RepostDestinationBuilder(context)
			.WithIsActive(true)
			.CreateAsync();

		await sut.UpdateDestinationAsync(destination.Id, false, ChatStatus.Active, 0, 0, 1, 0, null,
			CancellationToken.None);

		var updatedDestination = await context.RepostDestinations
			.FirstOrDefaultAsync(x => x.Id == destination.Id);

		updatedDestination.ShouldNotBeNull();
		updatedDestination.IsActive.ShouldBeFalse();
	}

	[Fact]
	public async Task UpdateDestinationAsync_ToActive_ShouldUpdateIsActiveAndChatStatus()
	{
		var destination = await new RepostDestinationBuilder(context)
			.WithIsActive(false)
			.WithChatStatus(ChatStatus.Left)
			.CreateAsync();

		await sut.UpdateDestinationAsync(destination.Id, true, ChatStatus.Active, 0, 0, 1, 0, null,
			CancellationToken.None);

		var updatedDestination = await context.RepostDestinations
			.AsNoTracking()
			.FirstOrDefaultAsync(x => x.Id == destination.Id);

		updatedDestination.ShouldNotBeNull();
		updatedDestination.IsActive.ShouldBeTrue();
		updatedDestination.ChatStatus.ShouldBe(ChatStatus.Active);
	}

	[Fact]
	public async Task GetDestinationAsync_ShouldReturnAccountBanFromRegistry()
	{
		var destination = await new RepostDestinationBuilder(context)
			.WithChatIdentifier(1375846471)
			.WithChatStatus(ChatStatus.Active)
			.CreateAsync();
		var sessionId = await context.RepostSettings
			.Where(x => x.Id == destination.RepostSettingsId)
			.Select(x => x.TelegramSessionId)
			.FirstAsync();
		await new TelegramSessionChatBanBuilder(context)
			.WithTelegramSessionId(sessionId)
			.WithChatId(1375846471)
			.CreateAsync();

		var result = await sut.GetDestinationAsync(destination.Id, CancellationToken.None);

		result.ShouldNotBeNull();
		result.IsAccountBanned.ShouldBeTrue();
		result.ChatStatus.ShouldBe(ChatStatus.Active);
	}

	[Fact]
	public async Task AddAccountBanAsync_Twice_ShouldKeepSingleBanWithLatestError()
	{
		var session = await new TelegramSessionBuilder(context).CreateAsync();

		await sut.AddAccountBanAsync(session.Id, 1375846471, "CHANNEL_PRIVATE", CancellationToken.None);
		await sut.AddAccountBanAsync(session.Id, 1375846471, "USER_BANNED_IN_CHANNEL", CancellationToken.None);

		var bans = await context.TelegramSessionChatBans
			.AsNoTracking()
			.Where(x => x.TelegramSessionId == session.Id)
			.ToListAsync();
		var ban = bans.ShouldHaveSingleItem();
		ban.ChatId.ShouldBe(1375846471);
		ban.Error.ShouldBe("USER_BANNED_IN_CHANNEL");
	}

	[Fact]
	public async Task RemoveAccountBanAsync_ShouldSoftDeleteBanAndAllowNewOne()
	{
		var ban = await new TelegramSessionChatBanBuilder(context).CreateAsync();

		await sut.RemoveAccountBanAsync(ban.TelegramSessionId, ban.ChatId, CancellationToken.None);
		await sut.AddAccountBanAsync(ban.TelegramSessionId, ban.ChatId, "CHANNEL_PRIVATE", CancellationToken.None);

		var bans = await context.TelegramSessionChatBans
			.IgnoreQueryFilters()
			.AsNoTracking()
			.Where(x => x.TelegramSessionId == ban.TelegramSessionId && x.ChatId == ban.ChatId)
			.ToListAsync();
		bans.Count.ShouldBe(2);
		bans.Single(x => x.Id == ban.Id).Deleted.ShouldNotBeNull();
		bans.Single(x => x.Id != ban.Id).Deleted.ShouldBeNull();
	}

	[Fact]
	public async Task RemoveAccountBanAsync_ShouldNotTouchOtherChatsAndSessions()
	{
		var ban = await new TelegramSessionChatBanBuilder(context).CreateAsync();
		var otherChat = await new TelegramSessionChatBanBuilder(context)
			.WithTelegramSessionId(ban.TelegramSessionId)
			.CreateAsync();
		var otherSession = await new TelegramSessionChatBanBuilder(context)
			.WithChatId(ban.ChatId)
			.CreateAsync();

		await sut.RemoveAccountBanAsync(ban.TelegramSessionId, ban.ChatId, CancellationToken.None);

		var activeIds = await context.TelegramSessionChatBans
			.AsNoTracking()
			.Where(x => x.Id == ban.Id || x.Id == otherChat.Id || x.Id == otherSession.Id)
			.Select(x => x.Id)
			.ToListAsync();
		activeIds.ShouldBe([otherChat.Id, otherSession.Id], true);
	}
}
