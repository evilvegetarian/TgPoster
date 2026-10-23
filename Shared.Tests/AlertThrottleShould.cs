using Shared.Alerts;
using Shouldly;

namespace Shared.Tests;

public sealed class AlertThrottleShould
{
	private static readonly TimeSpan Cooldown = TimeSpan.FromMinutes(30);

	private readonly ManualTimeProvider timeProvider = new(new DateTimeOffset(2026, 10, 24, 12, 0, 0, TimeSpan.Zero));
	private readonly AlertThrottle sut;

	public AlertThrottleShould()
	{
		sut = new AlertThrottle(timeProvider);
	}

	private enum TestProblem
	{
		First,
		Second
	}

	private enum OtherProblem
	{
		First
	}

	[Fact]
	public void Allow_WhenFirstAlert()
	{
		var result = sut.TryAcquire(Guid.NewGuid(), TestProblem.First, Cooldown);

		result.ShouldBeTrue();
	}

	[Fact]
	public void Deny_WhenRepeatedWithinCooldown()
	{
		var subjectId = Guid.NewGuid();
		sut.TryAcquire(subjectId, TestProblem.First, Cooldown);
		timeProvider.Advance(Cooldown - TimeSpan.FromSeconds(1));

		var result = sut.TryAcquire(subjectId, TestProblem.First, Cooldown);

		result.ShouldBeFalse();
	}

	[Fact]
	public void Allow_WhenOtherProblem()
	{
		var subjectId = Guid.NewGuid();
		sut.TryAcquire(subjectId, TestProblem.First, Cooldown);

		var result = sut.TryAcquire(subjectId, TestProblem.Second, Cooldown);

		result.ShouldBeTrue();
	}

	[Fact]
	public void Allow_WhenSameValueOfOtherEnum()
	{
		var subjectId = Guid.NewGuid();
		sut.TryAcquire(subjectId, TestProblem.First, Cooldown);

		var result = sut.TryAcquire(subjectId, OtherProblem.First, Cooldown);

		result.ShouldBeTrue();
	}

	[Fact]
	public void Allow_WhenOtherSubject()
	{
		sut.TryAcquire(Guid.NewGuid(), TestProblem.First, Cooldown);

		var result = sut.TryAcquire(Guid.NewGuid(), TestProblem.First, Cooldown);

		result.ShouldBeTrue();
	}

	[Fact]
	public void Allow_WhenCooldownPassed()
	{
		var subjectId = Guid.NewGuid();
		sut.TryAcquire(subjectId, TestProblem.First, Cooldown);
		timeProvider.Advance(Cooldown);

		var result = sut.TryAcquire(subjectId, TestProblem.First, Cooldown);

		result.ShouldBeTrue();
	}

	[Fact]
	public void Deny_WhenRepeatedRightAfterCooldownRestarted()
	{
		var subjectId = Guid.NewGuid();
		sut.TryAcquire(subjectId, TestProblem.First, Cooldown);
		timeProvider.Advance(Cooldown);
		sut.TryAcquire(subjectId, TestProblem.First, Cooldown);

		var result = sut.TryAcquire(subjectId, TestProblem.First, Cooldown);

		result.ShouldBeFalse();
	}

	private sealed class ManualTimeProvider(DateTimeOffset now) : TimeProvider
	{
		private DateTimeOffset now = now;

		public override DateTimeOffset GetUtcNow() => now;

		public void Advance(TimeSpan delta) => now += delta;
	}
}
