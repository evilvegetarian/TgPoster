using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Storages.CrossPosting;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public class RetryCrossPostStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context = fixture.GetDbContext();
	private readonly RetryCrossPostStorage sut = new(fixture.GetDbContext());

	private async Task<(Guid UserId, Guid MessageId, Guid CrossPostId)> CreateOwnedCrossPost()
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
			.WithStatus(CrossPostStatus.Failed)
			.CreateAsync();

		return (user.Id, message.Id, crossPost.Id);
	}

	[Fact]
	public async Task GetRetryInfoAsync_WithOwnedCrossPost_ShouldReturnInfo()
	{
		var (userId, messageId, crossPostId) = await CreateOwnedCrossPost();

		var result = await sut.GetRetryInfoAsync(messageId, crossPostId, userId, CancellationToken.None);

		result.ShouldNotBeNull();
		result.Status.ShouldBe(CrossPostStatus.Failed);
		result.AccountActive.ShouldBeTrue();
	}

	[Fact]
	public async Task GetRetryInfoAsync_WithAnotherUserMessage_ShouldReturnNull()
	{
		var (_, messageId, crossPostId) = await CreateOwnedCrossPost();

		var result = await sut.GetRetryInfoAsync(messageId, crossPostId, Guid.NewGuid(), CancellationToken.None);

		result.ShouldBeNull();
	}

	[Fact]
	public async Task GetRetryInfoAsync_WithAnotherMessage_ShouldReturnNull()
	{
		var (userId, _, crossPostId) = await CreateOwnedCrossPost();

		var result = await sut.GetRetryInfoAsync(Guid.NewGuid(), crossPostId, userId, CancellationToken.None);

		result.ShouldBeNull();
	}

	[Fact]
	public async Task GetRetryInfoAsync_WithInactiveAccount_ShouldReturnInactive()
	{
		var user = await new UserBuilder(context).CreateAsync();
		var schedule = await new ScheduleBuilder(context).WithUserId(user.Id).CreateAsync();
		var account = await new SocialAccountBuilder(context)
			.WithUserId(user.Id)
			.WithStatus(SocialAccountStatus.NeedsReauth)
			.CreateAsync();
		var message = await new MessageBuilder(context).WithSchedule(schedule).CreateAsync();
		var target = await new CrossPostTargetBuilder(context)
			.WithSchedule(schedule)
			.WithSocialAccount(account)
			.CreateAsync();
		var crossPost = await new CrossPostBuilder(context)
			.WithMessage(message)
			.WithTarget(target)
			.WithStatus(CrossPostStatus.Failed)
			.CreateAsync();

		var result = await sut.GetRetryInfoAsync(message.Id, crossPost.Id, user.Id, CancellationToken.None);

		result.ShouldNotBeNull();
		result.AccountActive.ShouldBeFalse();
	}

	[Fact]
	public async Task RetryAsync_ShouldResetCrossPost()
	{
		var (_, _, crossPostId) = await CreateOwnedCrossPost();
		var now = DateTimeOffset.UtcNow;

		await sut.RetryAsync(crossPostId, now, CancellationToken.None);

		var updated = await context.CrossPosts
			.AsNoTracking()
			.FirstAsync(x => x.Id == crossPostId);

		updated.Status.ShouldBe(CrossPostStatus.Pending);
		updated.Attempts.ShouldBe(0);
		updated.StartedAt.ShouldBeNull();
		updated.Error.ShouldBeNull();
		updated.ScheduledAt.ShouldBe(now, TimeSpan.FromMilliseconds(1));
	}
}
