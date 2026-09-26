using Shared.Enums;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Tests.Builders;

internal class CrossPostBuilder(PosterContext context)
{
	private readonly CrossPost crossPost = new()
	{
		Id = Guid.NewGuid(),
		MessageId = Guid.Empty,
		CrossPostTargetId = Guid.Empty,
		SocialAccountId = Guid.Empty,
		AccountName = "temp",
		Status = CrossPostStatus.Pending,
		ScheduledAt = DateTimeOffset.UtcNow,
		Attempts = 0
	};

	public CrossPostBuilder WithMessage(Message message)
	{
		crossPost.Message = message;
		crossPost.MessageId = message.Id;
		return this;
	}

	public CrossPostBuilder WithTarget(CrossPostTarget target)
	{
		crossPost.CrossPostTarget = target;
		crossPost.CrossPostTargetId = target.Id;
		crossPost.SocialAccountId = target.SocialAccountId;
		crossPost.Platform = target.SocialAccount?.Platform ?? SocialPlatform.Bluesky;
		crossPost.AccountName = target.SocialAccount?.Name ?? target.SocialAccountId.ToString();
		return this;
	}

	public CrossPostBuilder WithStatus(CrossPostStatus status)
	{
		crossPost.Status = status;
		return this;
	}

	public CrossPostBuilder WithScheduledAt(DateTimeOffset scheduledAt)
	{
		crossPost.ScheduledAt = scheduledAt;
		return this;
	}

	public CrossPostBuilder WithStartedAt(DateTimeOffset? startedAt)
	{
		crossPost.StartedAt = startedAt;
		return this;
	}

	public CrossPostBuilder WithAttempts(int attempts)
	{
		crossPost.Attempts = attempts;
		return this;
	}

	public CrossPost Build() => crossPost;

	public CrossPost Create()
	{
		context.CrossPosts.AddRange(crossPost);
		context.SaveChanges();
		return crossPost;
	}

	public async Task<CrossPost> CreateAsync(CancellationToken ct = default)
	{
		await context.CrossPosts.AddRangeAsync(crossPost);
		await context.SaveChangesAsync(ct);
		return crossPost;
	}
}
