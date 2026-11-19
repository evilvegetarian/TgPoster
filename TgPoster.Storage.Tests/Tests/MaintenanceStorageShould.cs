using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Storages;
using TgPoster.Storage.Storages.CleanupS3Files;
using TgPoster.Storage.Storages.CommentRepost;
using TgPoster.Storage.Storages.Repost;
using TgPoster.Storage.Tests.Builders;
using TgPoster.Telegram.Models;

namespace TgPoster.Storage.Tests.Tests;

public sealed class MaintenanceStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context = fixture.GetDbContext();

	[Fact]
	public async Task CreateUpdateAndDeleteOwnedProxy_AndDetachAffectedSessions()
	{
		var user = await new UserBuilder(context).CreateAsync();
		var id = await new CreateProxyStorage(context, new GuidFactory()).CreateAsync(user.Id, "proxy", ProxyType.Http,
			"proxy.example.test", 8080, "user", "password", null, CancellationToken.None);
		var session = new TelegramSessionBuilder(context).WithUserId(user.Id).Build();
		session.ProxyId = id;
		context.TelegramSessions.Add(session);
		await context.SaveChangesAsync();
		var update = new UpdateProxyStorage(context);
		(await update.ExistsAsync(user.Id, id, CancellationToken.None)).ShouldBeTrue();
		(await update.ExistsAsync(Guid.NewGuid(), id, CancellationToken.None)).ShouldBeFalse();

		var affected = await update.UpdateAsync(id, "updated", ProxyType.Socks5, "other.example.test", 1080,
			null, null, null, CancellationToken.None);
		var listed = await new ListProxiesStorage(context).GetByUserIdAsync(user.Id, CancellationToken.None);
		var deleted = await new DeleteProxyStorage(context).DeleteAsync(id, CancellationToken.None);

		affected.ShouldBe([session.Id]);
		listed.Single().Name.ShouldBe("updated");
		listed.Single().Host.ShouldBe("other.example.test");
		listed.Single().SessionsCount.ShouldBe(1);
		deleted.ShouldBe([session.Id]);
		(await context.TelegramSessions.AsNoTracking().FirstAsync(x => x.Id == session.Id)).ProxyId.ShouldBeNull();
		(await context.Proxies.AnyAsync(x => x.Id == id)).ShouldBeFalse();
	}

	[Fact]
	public async Task ResetS3Flags_OnlyForRequestedFiles()
	{
		var first = await new MessageFileBuilder(context).CreateAsync();
		var second = await new MessageFileBuilder(context).CreateAsync();
		first.IsInS3 = true;
		second.IsInS3 = true;
		await context.SaveChangesAsync();
		var sut = new CleanupS3FilesStorage(fixture.GetDbContext());

		await sut.ResetIsInS3FlagAsync([first.Id, Guid.NewGuid()], CancellationToken.None);

		(await context.MessageFiles.AsNoTracking().FirstAsync(x => x.Id == first.Id)).IsInS3.ShouldBeFalse();
		(await context.MessageFiles.AsNoTracking().FirstAsync(x => x.Id == second.Id)).IsInS3.ShouldBeTrue();
	}

	[Fact]
	public async Task IgnoreUnknownS3FileIds()
	{
		await new CleanupS3FilesStorage(context).ResetIsInS3FlagAsync([Guid.NewGuid()], CancellationToken.None);
		context.ChangeTracker.HasChanges().ShouldBeFalse();
	}

	[Fact]
	public async Task PersistImportedSessionAsActiveAndAuthorized()
	{
		var user = await new UserBuilder(context).CreateAsync();
		var sut = new ImportTelegramSessionStorage(fixture.GetDbContext(), new GuidFactory());

		var id = await sut.CreateAsync(user.Id, "123", "test-api-hash", "+10000000000", "Imported",
			"AQID", null, CancellationToken.None);

		var session = await context.TelegramSessions.AsNoTracking().FirstAsync(x => x.Id == id);
		session.UserId.ShouldBe(user.Id);
		session.IsActive.ShouldBeTrue();
		session.Status.ShouldBe(TgPoster.Storage.Data.Enum.TelegramSessionStatus.Authorized);
		session.SessionData.ShouldBe("AQID");
		session.Name.ShouldBe("Imported");
		session.NotificationBotId.ShouldBeNull();
	}

	[Fact]
	public async Task MonitorOnlyActiveCommentSettings_AndPersistCursor()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var session = await new TelegramSessionBuilder(context).WithUserId(schedule.UserId).CreateAsync();
		var active = new CommentRepostSettings
		{
			Id = Guid.NewGuid(), ScheduleId = schedule.Id, TelegramSessionId = session.Id,
			WatchedChannel = "channel", WatchedChannelId = 100, WatchedChannelAccessHash = 200,
			DiscussionGroupId = 300, DiscussionGroupAccessHash = 400, LastProcessedPostId = 10
		};
		var inactive = new CommentRepostSettings
		{
			Id = Guid.NewGuid(), ScheduleId = schedule.Id, TelegramSessionId = session.Id,
			WatchedChannel = "other", WatchedChannelId = 500, DiscussionGroupId = 600, IsActive = false
		};
		context.CommentRepostSettings.AddRange(active, inactive);
		await context.SaveChangesAsync();
		var sut = new CommentRepostMonitorStorage(fixture.GetDbContext());

		var settings = await sut.GetActiveSettingsAsync(CancellationToken.None);
		await sut.UpdateLastProcessedAsync(active.Id, 20, CancellationToken.None);

		settings.ShouldContain(x => x.Id == active.Id && x.SourceChannelId == schedule.ChannelId && x.LastProcessedPostId == 10);
		settings.ShouldNotContain(x => x.Id == inactive.Id);
		var saved = await context.CommentRepostSettings.AsNoTracking().FirstAsync(x => x.Id == active.Id);
		saved.LastProcessedPostId.ShouldBe(20);
		saved.LastCheckDate.ShouldNotBeNull();
	}

	[Theory]
	[InlineData(RepostImportStatus.Pending, 3, 0, true)]
	[InlineData(RepostImportStatus.Pending, 0, 0, false)]
	[InlineData(RepostImportStatus.InProgress, 60, 31, true)]
	[InlineData(RepostImportStatus.InProgress, 60, 5, false)]
	[InlineData(RepostImportStatus.CooldownWait, 1, 0, true)]
	[InlineData(RepostImportStatus.Completed, 60, 60, false)]
	public async Task ResumeOnlyLostOrStuckJobs_WithPendingItems(RepostImportStatus status, int createdMinutesAgo,
		int startedMinutesAgo, bool resumable)
	{
		var session = await new TelegramSessionBuilder(context).CreateAsync();
		var settings = await new RepostSettingsBuilder(context).WithTelegramSessionId(session.Id).CreateAsync();
		var job = new RepostImportJob
		{
			Id = Guid.NewGuid(), RepostSettingsId = settings.Id, Status = status,
			Created = DateTimeOffset.UtcNow.AddMinutes(-createdMinutesAgo), StartedAt = DateTimeOffset.UtcNow.AddMinutes(-startedMinutesAgo)
		};
		context.RepostImportJobs.Add(job);
		context.RepostImportJobItems.Add(new RepostImportJobItem
		{
			Id = Guid.NewGuid(), RepostImportJobId = job.Id, DiscoveredChannelId = Guid.NewGuid(),
			Title = "Pending", Outcome = AddDestinationOutcome.Pending
		});
		context.SaveChanges();
		var sut = new ResumeRepostImportJobsStorage(fixture.GetDbContext());

		var ids = await sut.GetResumableJobIdsAsync(CancellationToken.None);

		ids.Contains(job.Id).ShouldBe(resumable);
		session.FloodWaitUntil = DateTimeOffset.UtcNow.AddHours(1);
		await context.SaveChangesAsync();
		(await sut.GetResumableJobIdsAsync(CancellationToken.None)).ShouldNotContain(job.Id);
	}

	[Fact]
	public async Task NotResumeJobs_WhenThereAreNoPendingItems()
	{
		var settings = await new RepostSettingsBuilder(context).CreateAsync();
		var job = new RepostImportJob { Id = Guid.NewGuid(), RepostSettingsId = settings.Id, Status = RepostImportStatus.CooldownWait };
		context.RepostImportJobs.Add(job);
		context.RepostImportJobItems.Add(new RepostImportJobItem
		{
			Id = Guid.NewGuid(), RepostImportJobId = job.Id, DiscoveredChannelId = Guid.NewGuid(), Title = "Done",
			Outcome = AddDestinationOutcome.Added
		});
		await context.SaveChangesAsync();

		var ids = await new ResumeRepostImportJobsStorage(context).GetResumableJobIdsAsync(CancellationToken.None);

		ids.ShouldNotContain(job.Id);
	}
}
