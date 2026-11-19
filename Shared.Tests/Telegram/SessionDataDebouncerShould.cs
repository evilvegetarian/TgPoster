using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using TgPoster.Telegram.Abstractions;
using TgPoster.Telegram.Internal;

namespace Shared.Tests.Telegram;

public sealed class SessionDataDebouncerShould
{
	private readonly Mock<ITelegramAuthRepository> repository = new();
	private readonly ManualClock clock = new();

	[Fact]
	public async Task SaveOnlyLatestData_AfterFiveSecondsWithoutUpdates()
	{
		using var services = Services();
		await using var sut = Create(services);
		var id = Guid.NewGuid();
		sut.Update(id, [1]);
		clock.Advance(TimeSpan.FromSeconds(4));
		sut.Update(id, [2, 3]);
		clock.Advance(TimeSpan.FromSeconds(4));
		repository.VerifyNoOtherCalls();

		clock.Advance(TimeSpan.FromSeconds(1));

		repository.Verify(x => x.UpdateSessionDataAsync(id, Convert.ToBase64String(new byte[] { 2, 3 }),
			CancellationToken.None), Times.Once);
	}

	[Fact]
	public async Task FlushAfterThirtySeconds_EvenWhileUpdatesContinue()
	{
		using var services = Services();
		await using var sut = Create(services);
		var id = Guid.NewGuid();
		sut.Update(id, [0]);
		for (byte value = 1; value <= 7; value++)
		{
			clock.Advance(TimeSpan.FromSeconds(4));
			sut.Update(id, [value]);
		}
		repository.VerifyNoOtherCalls();

		clock.Advance(TimeSpan.FromSeconds(2));

		repository.Verify(x => x.UpdateSessionDataAsync(id, "Bw==", CancellationToken.None), Times.Once);
	}

	[Fact]
	public async Task FlushEachPendingSession_WhenDisposed()
	{
		using var services = Services();
		var sut = Create(services);
		var first = Guid.NewGuid();
		var second = Guid.NewGuid();
		sut.Update(first, [1]);
		sut.Update(second, [2]);

		await sut.DisposeAsync();
		clock.Advance(TimeSpan.FromMinutes(1));

		repository.Verify(x => x.UpdateSessionDataAsync(first, "AQ==", CancellationToken.None), Times.Once);
		repository.Verify(x => x.UpdateSessionDataAsync(second, "Ag==", CancellationToken.None), Times.Once);
	}

	[Fact]
	public async Task ContinueAfterStorageError_AndSaveNextUpdate()
	{
		using var services = Services();
		await using var sut = Create(services);
		var id = Guid.NewGuid();
		repository.SetupSequence(x => x.UpdateSessionDataAsync(id, It.IsAny<string>(), CancellationToken.None))
			.ThrowsAsync(new InvalidOperationException("database unavailable"))
			.Returns(Task.CompletedTask);
		sut.Update(id, [1]);
		clock.Advance(TimeSpan.FromSeconds(5));

		sut.Update(id, [2]);
		clock.Advance(TimeSpan.FromSeconds(5));

		repository.Verify(x => x.UpdateSessionDataAsync(id, "Ag==", CancellationToken.None), Times.Once);
	}

	[Fact]
	public async Task NotWriteAgain_WhenNothingChangedSinceLastFlush()
	{
		using var services = Services();
		await using var sut = Create(services);
		var id = Guid.NewGuid();
		sut.Update(id, [1]);

		clock.Advance(TimeSpan.FromMinutes(1));

		repository.Verify(x => x.UpdateSessionDataAsync(id, "AQ==", CancellationToken.None), Times.Once);
	}

	private ServiceProvider Services() => new ServiceCollection().AddSingleton(repository.Object).BuildServiceProvider();

	private SessionDataDebouncer Create(IServiceProvider services) => new(
		services.GetRequiredService<IServiceScopeFactory>(), NullLogger<SessionDataDebouncer>.Instance, clock);

	private sealed class ManualClock : TimeProvider
	{
		private readonly List<ManualTimer> timers = [];
		private long ticks;

		public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
		{
			var timer = new ManualTimer(this, callback, state);
			timers.Add(timer);
			timer.Change(dueTime, period);
			return timer;
		}

		public void Advance(TimeSpan duration)
		{
			var target = ticks + duration.Ticks;
			while (timers.Where(t => t.Due <= target).OrderBy(t => t.Due).FirstOrDefault() is { } timer)
			{
				ticks = timer.Due;
				timer.Fire();
			}
			ticks = target;
		}

		private sealed class ManualTimer(ManualClock clock, TimerCallback callback, object? state) : ITimer
		{
			private bool disposed;
			public long Due { get; private set; } = long.MaxValue;

			public bool Change(TimeSpan dueTime, TimeSpan period)
			{
				ObjectDisposedException.ThrowIf(disposed, this);
				Due = dueTime == Timeout.InfiniteTimeSpan ? long.MaxValue : clock.ticks + dueTime.Ticks;
				return true;
			}

			public void Fire()
			{
				Due = long.MaxValue;
				callback(state);
			}

			public void Dispose() { disposed = true; Due = long.MaxValue; }
			public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
		}
	}
}
