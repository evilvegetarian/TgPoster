using Microsoft.EntityFrameworkCore;
using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Storages.CommentRepost;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public sealed class SendCommentConsumerStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context = fixture.GetDbContext();
	private readonly CancellationToken ct = CancellationToken.None;
	private readonly SendCommentConsumerStorage sut = new(fixture.GetDbContext(), new GuidFactory());

	[Fact]
	public async Task IsAccountBannedAsync_ShouldConsiderOnlyActiveBanOfSession()
	{
		var ban = await new TelegramSessionChatBanBuilder(context).CreateAsync(ct);
		var removed = await new TelegramSessionChatBanBuilder(context)
			.WithDeleted(DateTimeOffset.UtcNow)
			.CreateAsync(ct);

		(await sut.IsAccountBannedAsync(ban.TelegramSessionId, ban.ChatId, ct)).ShouldBeTrue();
		(await sut.IsAccountBannedAsync(Guid.NewGuid(), ban.ChatId, ct)).ShouldBeFalse();
		(await sut.IsAccountBannedAsync(removed.TelegramSessionId, removed.ChatId, ct)).ShouldBeFalse();
	}

	[Fact]
	public async Task MarkAccountBannedAsync_ShouldRecordBanAndDisableSettingsOfSessionInGroup()
	{
		var session = await new TelegramSessionBuilder(context).CreateAsync(ct);
		var groupId = Random.Shared.NextInt64(1_000_000, long.MaxValue);
		var first = await CreateSettingsAsync(session.Id, groupId);
		var second = await CreateSettingsAsync(session.Id, groupId);
		var otherGroup = await CreateSettingsAsync(session.Id, groupId - 1);
		var otherSession = await CreateSettingsAsync((await new TelegramSessionBuilder(context).CreateAsync(ct)).Id,
			groupId);

		await sut.MarkAccountBannedAsync(session.Id, groupId, "USER_BANNED_IN_CHANNEL", ct);

		var ban = await context.TelegramSessionChatBans
			.AsNoTracking()
			.SingleAsync(x => x.TelegramSessionId == session.Id, ct);
		ban.ChatId.ShouldBe(groupId);
		ban.Error.ShouldBe("USER_BANNED_IN_CHANNEL");
		(await IsActiveAsync(first.Id)).ShouldBeFalse();
		(await IsActiveAsync(second.Id)).ShouldBeFalse();
		(await IsActiveAsync(otherGroup.Id)).ShouldBeTrue();
		(await IsActiveAsync(otherSession.Id)).ShouldBeTrue();
	}

	[Fact]
	public async Task MarkChatWriteForbiddenAsync_ShouldDisableSettingsAndRecordRestrictionInDiscover()
	{
		var session = await new TelegramSessionBuilder(context).CreateAsync(ct);
		var groupId = Random.Shared.NextInt64(1_000_000, long.MaxValue);
		var settings = await CreateSettingsAsync(session.Id, groupId);
		var other = await CreateSettingsAsync(session.Id, groupId);
		var group = new DiscoveredChannel
		{
			Id = Guid.NewGuid(),
			TelegramId = groupId,
			Status = Data.Enum.DiscoveryStatus.Completed,
			CanSendMessages = true,
			CanSendMedia = true
		};
		await context.DiscoveredChannels.AddAsync(group, ct);
		await context.SaveChangesAsync(ct);

		await sut.MarkChatWriteForbiddenAsync(settings.Id, groupId, ct);

		(await IsActiveAsync(settings.Id)).ShouldBeFalse();
		(await IsActiveAsync(other.Id)).ShouldBeTrue();
		var channel = await context.DiscoveredChannels.AsNoTracking().SingleAsync(x => x.Id == group.Id, ct);
		channel.CanSendMessages.ShouldBe(false);
		channel.CanSendMedia.ShouldBe(true);
		(await context.TelegramSessionChatBans.AnyAsync(x => x.TelegramSessionId == session.Id, ct)).ShouldBeFalse();
	}

	private async Task<CommentRepostSettings> CreateSettingsAsync(Guid sessionId, long discussionGroupId)
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync(ct);
		var settings = new CommentRepostSettings
		{
			Id = Guid.NewGuid(),
			WatchedChannel = "@watched",
			WatchedChannelId = Random.Shared.NextInt64(1_000_000, long.MaxValue),
			DiscussionGroupId = discussionGroupId,
			TelegramSessionId = sessionId,
			ScheduleId = schedule.Id,
			IsActive = true
		};

		await context.CommentRepostSettings.AddAsync(settings, ct);
		await context.SaveChangesAsync(ct);

		return settings;
	}

	private Task<bool> IsActiveAsync(Guid id) =>
		context.CommentRepostSettings.AsNoTracking().Where(x => x.Id == id).Select(x => x.IsActive).FirstAsync(ct);
}
