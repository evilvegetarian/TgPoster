using Microsoft.EntityFrameworkCore;
using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Storages.CommentRepost;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public sealed class UpdateCommentRepostStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context = fixture.GetDbContext();
	private readonly CancellationToken ct = CancellationToken.None;
	private readonly UpdateCommentRepostStorage sut = new(fixture.GetDbContext());

	[Fact]
	public async Task RemoveAccountBanAsync_ShouldRemoveBanOfSessionInDiscussionGroupOnly()
	{
		var groupBan = await new TelegramSessionChatBanBuilder(context).CreateAsync(ct);
		var otherChatBan = await new TelegramSessionChatBanBuilder(context)
			.WithTelegramSessionId(groupBan.TelegramSessionId)
			.CreateAsync(ct);
		var settings = await CreateSettingsAsync(groupBan.TelegramSessionId, groupBan.ChatId);

		await sut.RemoveAccountBanAsync(settings.Id, ct);

		var activeIds = await context.TelegramSessionChatBans
			.AsNoTracking()
			.Where(x => x.TelegramSessionId == groupBan.TelegramSessionId)
			.Select(x => x.Id)
			.ToListAsync(ct);
		activeIds.ShouldBe([otherChatBan.Id]);
	}

	[Fact]
	public async Task RemoveAccountBanAsync_WithoutBan_ShouldDoNothing()
	{
		var session = await new TelegramSessionBuilder(context).CreateAsync(ct);
		var settings = await CreateSettingsAsync(session.Id, 1375846471);

		await Should.NotThrowAsync(() => sut.RemoveAccountBanAsync(settings.Id, ct));
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
			IsActive = false
		};

		await context.CommentRepostSettings.AddAsync(settings, ct);
		await context.SaveChangesAsync(ct);

		return settings;
	}
}
