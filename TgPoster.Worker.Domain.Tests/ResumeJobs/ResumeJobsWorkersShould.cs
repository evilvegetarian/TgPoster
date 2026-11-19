using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shared.Telegram;
using Shouldly;
using TgPoster.Worker.Domain.UseCases.CleanupChannels;
using TgPoster.Worker.Domain.UseCases.ImportRepostDestinations;

namespace TgPoster.Worker.Domain.Tests.ResumeJobs;

public sealed class ResumeJobsWorkersShould
{
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task PublishOnlyResumableJobs_WithShutdownToken(bool cleanup)
	{
		using var stopping = new CancellationTokenSource();
		var lifetime = Mock.Of<IHostApplicationLifetime>(x => x.ApplicationStopping == stopping.Token);
		var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
		var bus = new Mock<IBus>();
		var published = new List<Guid>();
		bus.Setup(x => x.Publish(It.IsAny<CleanupChannelsContract>(), stopping.Token))
			.Callback<CleanupChannelsContract, CancellationToken>((message, _) => published.Add(message.JobId))
			.Returns(Task.CompletedTask);
		bus.Setup(x => x.Publish(It.IsAny<ImportRepostDestinationsContract>(), stopping.Token))
			.Callback<ImportRepostDestinationsContract, CancellationToken>((message, _) => published.Add(message.JobId))
			.Returns(Task.CompletedTask);

		if (cleanup)
		{
			var storage = new Mock<IResumeChannelCleanupJobsStorage>();
			storage.Setup(x => x.GetResumableJobIdsAsync(stopping.Token)).ReturnsAsync(ids);
			await new ResumeChannelCleanupJobsWorker(storage.Object, bus.Object,
				NullLogger<ResumeChannelCleanupJobsWorker>.Instance, lifetime).ResumeAsync();
		}
		else
		{
			var storage = new Mock<IResumeRepostImportJobsStorage>();
			storage.Setup(x => x.GetResumableJobIdsAsync(stopping.Token)).ReturnsAsync(ids);
			await new ResumeRepostImportJobsWorker(storage.Object, bus.Object,
				NullLogger<ResumeRepostImportJobsWorker>.Instance, lifetime).ResumeAsync();
		}

		published.ShouldBe(ids);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task NotPublish_WhenQueueIsEmpty(bool cleanup)
	{
		var bus = new Mock<IBus>(MockBehavior.Strict);
		var lifetime = Mock.Of<IHostApplicationLifetime>();

		if (cleanup)
		{
			var storage = new Mock<IResumeChannelCleanupJobsStorage>();
			storage.Setup(x => x.GetResumableJobIdsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
			await new ResumeChannelCleanupJobsWorker(storage.Object, bus.Object,
				NullLogger<ResumeChannelCleanupJobsWorker>.Instance, lifetime).ResumeAsync();
		}
		else
		{
			var storage = new Mock<IResumeRepostImportJobsStorage>();
			storage.Setup(x => x.GetResumableJobIdsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
			await new ResumeRepostImportJobsWorker(storage.Object, bus.Object,
				NullLogger<ResumeRepostImportJobsWorker>.Instance, lifetime).ResumeAsync();
		}

		bus.VerifyNoOtherCalls();
	}
}
