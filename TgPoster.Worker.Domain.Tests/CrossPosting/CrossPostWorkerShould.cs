using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using TgPoster.Worker.Domain.UseCases.CrossPosting;

namespace TgPoster.Worker.Domain.Tests.CrossPosting;

public class CrossPostWorkerShould
{
	private static readonly DateTimeOffset FixedNow = new(2026, 9, 26, 12, 0, 0, TimeSpan.Zero);

	private readonly Mock<ICrossPostWorkerStorage> storage;
	private readonly Mock<ICrossPostPublisher> publisher;
	private readonly FakeLifetime lifetime;

	public CrossPostWorkerShould()
	{
		storage = new Mock<ICrossPostWorkerStorage>();
		publisher = new Mock<ICrossPostPublisher>();
		lifetime = new FakeLifetime();
	}

	[Fact]
	public async Task Process_ShouldEnqueueFailStuckAndSkipOrphaned()
	{
		var sut = CreateSut();
		storage.Setup(s => s.TakeNextDueAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync((Guid?)null);

		await sut.ProcessAsync();

		storage.Verify(s => s.EnqueueDueAsync(FixedNow, It.IsAny<CancellationToken>()), Times.Once);
		storage.Verify(
			s => s.FailStuckAsync(
				FixedNow.AddMinutes(-15),
				"Публикация прервана (перезапуск воркера) — проверьте пост в соцсети вручную",
				It.IsAny<CancellationToken>()),
			Times.Once);
		storage.Verify(s => s.SkipOrphanedAsync(It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task Process_ShouldPublishUntilNull()
	{
		var ids = new Queue<Guid?>();
		ids.Enqueue(Guid.NewGuid());
		ids.Enqueue(Guid.NewGuid());
		ids.Enqueue(Guid.NewGuid());
		ids.Enqueue(null);
		storage.Setup(s => s.TakeNextDueAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(() => ids.Dequeue());
		var sut = CreateSut();

		await sut.ProcessAsync();

		publisher.Verify(p => p.PublishAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
	}

	[Fact]
	public async Task Process_ShouldNotExceedTenPublications()
	{
		var ids = new Queue<Guid?>();
		for (var i = 0; i < 12; i++)
		{
			ids.Enqueue(Guid.NewGuid());
		}

		ids.Enqueue(null);
		storage.Setup(s => s.TakeNextDueAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(() => ids.Dequeue());
		var sut = CreateSut();

		await sut.ProcessAsync();

		publisher.Verify(p => p.PublishAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Exactly(10));
	}

	[Fact]
	public async Task Process_WhenPublisherThrows_ShouldMarkFailedAndContinue()
	{
		var firstId = Guid.NewGuid();
		var secondId = Guid.NewGuid();
		var ids = new Queue<Guid?>();
		ids.Enqueue(firstId);
		ids.Enqueue(secondId);
		ids.Enqueue(null);
		storage.Setup(s => s.TakeNextDueAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(() => ids.Dequeue());
		publisher
			.Setup(p => p.PublishAsync(firstId, It.IsAny<CancellationToken>()))
			.ThrowsAsync(new InvalidOperationException("boom"));
		var sut = CreateSut();

		await sut.ProcessAsync();

		storage.Verify(s => s.MarkFailedAsync(firstId, "Непредвиденная ошибка публикации", It.IsAny<CancellationToken>()), Times.Once);
		publisher.Verify(p => p.PublishAsync(secondId, It.IsAny<CancellationToken>()), Times.Once);
	}

	private CrossPostWorker CreateSut()
	{
		return new CrossPostWorker(
			storage.Object,
			publisher.Object,
			new FixedTimeProvider(FixedNow),
			NullLogger<CrossPostWorker>.Instance,
			lifetime);
	}

	private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
	{
		public override DateTimeOffset GetUtcNow() => now;
	}

	private sealed class FakeLifetime : IHostApplicationLifetime
	{
		public CancellationToken ApplicationStarted => CancellationToken.None;
		public CancellationToken ApplicationStopping => CancellationToken.None;
		public CancellationToken ApplicationStopped => CancellationToken.None;

		public void StopApplication()
		{
		}
	}
}
