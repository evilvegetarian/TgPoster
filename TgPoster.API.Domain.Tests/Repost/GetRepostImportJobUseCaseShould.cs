using Moq;
using Security.IdentityServices;
using Shared.Enums;
using Shouldly;
using TgPoster.API.Domain.UseCases.Repost.GetRepostImportJob;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.Tests.Repost;

public sealed class GetRepostImportJobUseCaseShould
{
	private readonly Guid userId = Guid.NewGuid();
	private readonly Guid jobId = Guid.NewGuid();
	private readonly Mock<IGetRepostImportJobStorage> storage = new();
	private readonly GetRepostImportJobUseCase sut;

	public GetRepostImportJobUseCaseShould()
	{
		var identity = new Mock<IIdentityProvider>();
		identity.SetupGet(x => x.Current).Returns(new Identity(userId));
		sut = new(storage.Object, identity.Object);
	}

	[Fact]
	public async Task RejectMissingOrForeignJob_UsingCurrentOwner()
	{
		await Should.ThrowAsync<RepostImportJobNotFoundException>(() => sut.Handle(new(jobId), CancellationToken.None));

		storage.Verify(x => x.GetJobStateAsync(jobId, userId, CancellationToken.None), Times.Once);
	}

	[Fact]
	public async Task CountOutcomes_AndPreserveIndividualDestinationResults()
	{
		var destination = Guid.NewGuid();
		var added = new RepostImportJobItemState(Guid.NewGuid(), "Added", AddDestinationOutcome.Added, destination, null);
		var pending = new RepostImportJobItemState(Guid.NewGuid(), "Pending", AddDestinationOutcome.Pending, null, null);
		var rejected = new RepostImportJobItemState(Guid.NewGuid(), "Rejected", AddDestinationOutcome.NoWritePermission, null, "no permission");
		storage.Setup(x => x.GetJobStateAsync(jobId, userId, CancellationToken.None))
			.ReturnsAsync(new RepostImportJobState(jobId, RepostImportStatus.InProgress, null, [added, pending, rejected]));

		var result = await sut.Handle(new(jobId), CancellationToken.None);

		result.JobId.ShouldBe(jobId);
		result.Status.ShouldBe(RepostImportStatus.InProgress);
		result.TotalCount.ShouldBe(3);
		result.AddedCount.ShouldBe(1);
		result.PendingCount.ShouldBe(1);
		result.SkippedCount.ShouldBe(1);
		result.Results[0].DestinationId.ShouldBe(destination);
		result.Results[0].DiscoveredChannelId.ShouldBe(added.DiscoveredChannelId);
		result.Results[2].Error.ShouldBe("no permission");
		result.Results[2].Title.ShouldBe("Rejected");
	}

	[Theory]
	[InlineData(RepostImportStatus.CooldownWait, 60, true)]
	[InlineData(RepostImportStatus.CooldownWait, -60, false)]
	[InlineData(RepostImportStatus.CooldownWait, null, false)]
	[InlineData(RepostImportStatus.Completed, 60, false)]
	public async Task ReportRetryDelay_OnlyForActiveCooldown(RepostImportStatus status, int? seconds, bool expected)
	{
		storage.Setup(x => x.GetJobStateAsync(jobId, userId, CancellationToken.None))
			.ReturnsAsync(new RepostImportJobState(jobId, status, seconds is null ? null : DateTimeOffset.UtcNow.AddSeconds(seconds.Value), []));

		var result = await sut.Handle(new(jobId), CancellationToken.None);

		if (expected)
		{
			result.RetryAfterSeconds.ShouldNotBeNull();
			result.RetryAfterSeconds.Value.ShouldBeInRange(55, 60);
		}
		else result.RetryAfterSeconds.ShouldBeNull();
		result.TotalCount.ShouldBe(0);
		result.Results.ShouldBeEmpty();
	}
}
