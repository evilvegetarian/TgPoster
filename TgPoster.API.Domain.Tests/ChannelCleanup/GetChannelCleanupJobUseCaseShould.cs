using Moq;
using Security.IdentityServices;
using Shared.Enums;
using Shouldly;
using TgPoster.API.Domain.UseCases.ChannelCleanup.GetChannelCleanupJob;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.Tests.ChannelCleanup;

public sealed class GetChannelCleanupJobUseCaseShould
{
	private readonly Guid jobId = Guid.NewGuid();
	private readonly Mock<IGetChannelCleanupJobStorage> storage;
	private readonly GetChannelCleanupJobUseCase sut;
	private readonly Guid userId = Guid.NewGuid();

	public GetChannelCleanupJobUseCaseShould()
	{
		storage = new Mock<IGetChannelCleanupJobStorage>();
		var identityProvider = new Mock<IIdentityProvider>();
		identityProvider.Setup(x => x.Current).Returns(new Identity(userId));

		sut = new GetChannelCleanupJobUseCase(storage.Object, identityProvider.Object);
	}

	[Fact]
	public async Task CountOutcomes()
	{
		SetupJob(ChannelCleanupStatus.InProgress, null,
			ChannelCleanupOutcome.Left,
			ChannelCleanupOutcome.NotMember,
			ChannelCleanupOutcome.InUse,
			ChannelCleanupOutcome.Failed,
			ChannelCleanupOutcome.NotProcessed,
			ChannelCleanupOutcome.Pending);

		var result = await sut.Handle(new GetChannelCleanupJobQuery(jobId), CancellationToken.None);

		result.TotalCount.ShouldBe(6);
		result.LeftCount.ShouldBe(2);
		result.SkippedCount.ShouldBe(1);
		result.FailedCount.ShouldBe(2);
		result.PendingCount.ShouldBe(1);
		result.RetryAfterSeconds.ShouldBeNull();
	}

	[Fact]
	public async Task ReportRetryAfter_WhenJobWaitsForCooldown()
	{
		SetupJob(ChannelCleanupStatus.CooldownWait, DateTimeOffset.UtcNow.AddMinutes(10),
			ChannelCleanupOutcome.Pending);

		var result = await sut.Handle(new GetChannelCleanupJobQuery(jobId), CancellationToken.None);

		result.RetryAfterSeconds.ShouldNotBeNull();
		result.RetryAfterSeconds.Value.ShouldBeInRange(590, 600);
	}

	[Fact]
	public async Task ThrowNotFound_WhenJobIsMissingOrForeign()
	{
		await Should.ThrowAsync<ChannelCleanupJobNotFoundException>(async () =>
			await sut.Handle(new GetChannelCleanupJobQuery(jobId), CancellationToken.None));
	}

	private void SetupJob(
		ChannelCleanupStatus status,
		DateTimeOffset? floodWaitUntil,
		params ChannelCleanupOutcome[] outcomes
	) =>
		storage.Setup(s => s.GetJobStateAsync(jobId, userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new ChannelCleanupJobState(
				jobId,
				status,
				floodWaitUntil,
				null,
				outcomes
					.Select((x, i) => new ChannelCleanupItemResponse
					{
						ChatId = i + 1,
						Title = $"Канал {i + 1}",
						ChatType = ChatType.Channel,
						Outcome = x
					})
					.ToList()));
}
