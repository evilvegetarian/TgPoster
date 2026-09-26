using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.Enum;
using TgPoster.Storage.Storages.CrossPosting;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public class CrossPostWorkerStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context = fixture.GetDbContext();
	private readonly CrossPostWorkerStorage sut = new(fixture.GetDbContext(), new GuidFactory());
	private readonly TimeProvider timeProvider = TimeProvider.System;

	[Fact]
	public async Task EnqueueDueAsync_ShouldCreateCrossPostForSentMessageAndActiveTarget()
	{
		var now = DateTimeOffset.UtcNow;
		var user = await new UserBuilder(context).CreateAsync();
		var schedule = await new ScheduleBuilder(context).WithUserId(user.Id).CreateAsync();
		var account = await new SocialAccountBuilder(context).WithUserId(user.Id).CreateAsync();
		var target = await new CrossPostTargetBuilder(context)
			.WithSchedule(schedule)
			.WithSocialAccount(account)
			.WithDelayMinutes(5)
			.CreateAsync();
		var message = await new MessageBuilder(context)
			.WithSchedule(schedule)
			.WithStatus(MessageStatus.Send)
			.WithTelegramMessageId(100)
			.WithCrossPostEnabled(true)
			.WithTimeMessage(now.AddMinutes(10))
			.CreateAsync();

		var count = await sut.EnqueueDueAsync(now, CancellationToken.None);

		count.ShouldBe(1);
		var crossPost = await context.CrossPosts.AsNoTracking().FirstAsync(x => x.MessageId == message.Id);
		crossPost.Status.ShouldBe(CrossPostStatus.Pending);
		crossPost.ScheduledAt.ShouldBe(message.TimePosting.AddMinutes(target.DelayMinutes), TimeSpan.FromMilliseconds(1));
		crossPost.SocialAccountId.ShouldBe(account.Id);
	}

	[Fact]
	public async Task EnqueueDueAsync_ShouldNotCreateDuplicate()
	{
		var now = DateTimeOffset.UtcNow;
		var user = await new UserBuilder(context).CreateAsync();
		var schedule = await new ScheduleBuilder(context).WithUserId(user.Id).CreateAsync();
		var account = await new SocialAccountBuilder(context).WithUserId(user.Id).CreateAsync();
		var target = await new CrossPostTargetBuilder(context)
			.WithSchedule(schedule)
			.WithSocialAccount(account)
			.CreateAsync();
		var message = await new MessageBuilder(context)
			.WithSchedule(schedule)
			.WithStatus(MessageStatus.Send)
			.WithTelegramMessageId(100)
			.WithCrossPostEnabled(true)
			.WithTimeMessage(now.AddMinutes(10))
			.CreateAsync();
		await sut.EnqueueDueAsync(now, CancellationToken.None);

		var secondCount = await sut.EnqueueDueAsync(now, CancellationToken.None);

		secondCount.ShouldBe(0);
		context.CrossPosts.AsNoTracking().Count(x => x.MessageId == message.Id).ShouldBe(1);
	}

	[Fact]
	public async Task EnqueueDueAsync_ShouldSkipParsedMessageWithoutIncludeParsed()
	{
		var now = DateTimeOffset.UtcNow;
		var user = await new UserBuilder(context).CreateAsync();
		var schedule = await new ScheduleBuilder(context).WithUserId(user.Id).CreateAsync();
		var account = await new SocialAccountBuilder(context).WithUserId(user.Id).CreateAsync();
		await new CrossPostTargetBuilder(context)
			.WithSchedule(schedule)
			.WithSocialAccount(account)
			.WithIncludeParsed(false)
			.CreateAsync();
		await new MessageBuilder(context)
			.WithSchedule(schedule)
			.WithStatus(MessageStatus.Send)
			.WithTelegramMessageId(100)
			.WithCrossPostEnabled(true)
			.WithChannelParsingSettingId(Guid.NewGuid())
			.WithTimeMessage(now.AddMinutes(10))
			.CreateAsync();

		var count = await sut.EnqueueDueAsync(now, CancellationToken.None);

		count.ShouldBe(0);
	}

	[Fact]
	public async Task EnqueueDueAsync_ShouldSkipTargetCreatedAfterMessage()
	{
		var now = DateTimeOffset.UtcNow;
		var user = await new UserBuilder(context).CreateAsync();
		var schedule = await new ScheduleBuilder(context).WithUserId(user.Id).CreateAsync();
		var account = await new SocialAccountBuilder(context).WithUserId(user.Id).CreateAsync();
		await new MessageBuilder(context)
			.WithSchedule(schedule)
			.WithStatus(MessageStatus.Send)
			.WithTelegramMessageId(100)
			.WithCrossPostEnabled(true)
			.WithTimeMessage(now.AddMinutes(-1))
			.CreateAsync();
		await new CrossPostTargetBuilder(context)
			.WithSchedule(schedule)
			.WithSocialAccount(account)
			.CreateAsync();

		var count = await sut.EnqueueDueAsync(now, CancellationToken.None);

		count.ShouldBe(0);
	}

	[Fact]
	public async Task TakeNextDueAsync_ShouldTakeEarliestAndSetInProgress()
	{
		var now = DateTimeOffset.UtcNow;
		var user = await new UserBuilder(context).CreateAsync();
		var schedule = await new ScheduleBuilder(context).WithUserId(user.Id).CreateAsync();
		var account = await new SocialAccountBuilder(context).WithUserId(user.Id).CreateAsync();
		var message = await new MessageBuilder(context).WithSchedule(schedule).CreateAsync();
		var target = await new CrossPostTargetBuilder(context)
			.WithSchedule(schedule)
			.WithSocialAccount(account)
			.CreateAsync();
		var early = await new CrossPostBuilder(context)
			.WithMessage(message)
			.WithTarget(target)
			.WithScheduledAt(now.AddMinutes(-5))
			.CreateAsync();
		await new CrossPostBuilder(context)
			.WithMessage(message)
			.WithTarget(target)
			.WithScheduledAt(now.AddMinutes(5))
			.CreateAsync();

		var id = await sut.TakeNextDueAsync(now, CancellationToken.None);

		id.ShouldBe(early.Id);
		var updated = await context.CrossPosts.AsNoTracking().FirstAsync(x => x.Id == early.Id);
		updated.Status.ShouldBe(CrossPostStatus.InProgress);
		updated.Attempts.ShouldBe(1);
		updated.StartedAt.ShouldNotBeNull();
		updated.StartedAt!.Value.ShouldBe(now, TimeSpan.FromMilliseconds(1));
	}

	[Fact]
	public async Task FailStuckAsync_ShouldMarkOldInProgressAsFailed()
	{
		var now = DateTimeOffset.UtcNow;
		var user = await new UserBuilder(context).CreateAsync();
		var schedule = await new ScheduleBuilder(context).WithUserId(user.Id).CreateAsync();
		var account = await new SocialAccountBuilder(context).WithUserId(user.Id).CreateAsync();
		var message = await new MessageBuilder(context).WithSchedule(schedule).CreateAsync();
		var target = await new CrossPostTargetBuilder(context)
			.WithSchedule(schedule)
			.WithSocialAccount(account)
			.CreateAsync();
		var crossPost = await new CrossPostBuilder(context)
			.WithMessage(message)
			.WithTarget(target)
			.WithStatus(CrossPostStatus.InProgress)
			.WithStartedAt(now.AddMinutes(-20))
			.CreateAsync();

		var count = await sut.FailStuckAsync(now.AddMinutes(-15), "stuck", CancellationToken.None);

		count.ShouldBe(1);
		var updated = await context.CrossPosts.AsNoTracking().FirstAsync(x => x.Id == crossPost.Id);
		updated.Status.ShouldBe(CrossPostStatus.Failed);
		updated.Error.ShouldBe("stuck");
	}

	[Fact]
	public async Task SkipOrphanedAsync_WhenAccountDeleted_ShouldSkip()
	{
		var user = await new UserBuilder(context).CreateAsync();
		var schedule = await new ScheduleBuilder(context).WithUserId(user.Id).CreateAsync();
		var account = await new SocialAccountBuilder(context).WithUserId(user.Id).CreateAsync();
		var message = await new MessageBuilder(context).WithSchedule(schedule).CreateAsync();
		var target = await new CrossPostTargetBuilder(context)
			.WithSchedule(schedule)
			.WithSocialAccount(account)
			.CreateAsync();
		var crossPost = await new CrossPostBuilder(context)
			.WithMessage(message)
			.WithTarget(target)
			.WithStatus(CrossPostStatus.Pending)
			.CreateAsync();

		context.SocialAccounts.Remove(account);
		await context.SaveChangesAsync();

		var count = await sut.SkipOrphanedAsync(CancellationToken.None);

		count.ShouldBe(1);
		var updated = await context.CrossPosts.AsNoTracking().FirstAsync(x => x.Id == crossPost.Id);
		updated.Status.ShouldBe(CrossPostStatus.Skipped);
		updated.Error.ShouldBe("Аккаунт соцсети удалён или требует переподключения");
	}
}
