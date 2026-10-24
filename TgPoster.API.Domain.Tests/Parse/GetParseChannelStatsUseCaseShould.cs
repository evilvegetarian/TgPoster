using Moq;
using Security.IdentityServices;
using Shouldly;
using TgPoster.API.Domain.UseCases.Parse.GetParseChannelStats;
using TgPoster.Exceptions.BadRequest;

namespace TgPoster.API.Domain.Tests.Parse;

public sealed class GetParseChannelStatsUseCaseShould
{
	private static readonly DateTimeOffset Now = new(2026, 10, 25, 12, 0, 0, TimeSpan.Zero);

	private readonly Mock<IGetParseChannelStatsStorage> storage = new();
	private readonly Guid userId = Guid.NewGuid();
	private readonly GetParseChannelStatsUseCase sut;

	public GetParseChannelStatsUseCaseShould()
	{
		storage.Setup(s => s.GetSourceStatusCountsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);
		storage.Setup(s => s.GetPostTotalsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(new ParseChannelPostTotals
			{
				Total = 0, Planned = 0, AwaitingApproval = 0, Published = 0, Failed = 0, Canceled = 0
			});
		storage.Setup(s => s.GetRunTotalsAsync(It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(),
				It.IsAny<CancellationToken>()))
			.ReturnsAsync(ParseChannelRunTotals.Empty);
		storage.Setup(s => s.GetRunsByDayAsync(It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(),
				It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);
		storage.Setup(s => s.GetTopSourcesAsync(It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<int>(),
				It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);

		var identity = new Mock<IIdentityProvider>();
		identity.Setup(x => x.Current).Returns(new Identity(userId));
		sut = new GetParseChannelStatsUseCase(storage.Object, identity.Object, new FixedTimeProvider(Now));
	}

	[Theory]
	[InlineData(0)]
	[InlineData(GetParseChannelStatsUseCase.MaxDays + 1)]
	public async Task ThrowInvalidPeriod_WhenDaysOutOfRange(int days)
	{
		await Should.ThrowAsync<InvalidDiscoverStatsPeriodException>(
			() => sut.Handle(new GetParseChannelStatsQuery(days), CancellationToken.None));

		storage.Verify(s => s.GetSourceStatusCountsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
			Times.Never);
	}

	[Fact]
	public async Task CountSourcesAndFillAllStatuses()
	{
		storage.Setup(s => s.GetSourceStatusCountsAsync(userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(
			[
				new ParseChannelSourceStatusCount { Status = ParseChannelSourceStatus.Waiting, Count = 3 },
				new ParseChannelSourceStatusCount { Status = ParseChannelSourceStatus.Failed, Count = 1 },
				new ParseChannelSourceStatusCount { Status = ParseChannelSourceStatus.Finished, Count = 2 }
			]);

		var result = await sut.Handle(new GetParseChannelStatsQuery(30), CancellationToken.None);

		result.Sources.Total.ShouldBe(6);
		result.Sources.Active.ShouldBe(4);
		result.Sources.Failed.ShouldBe(1);
		result.SourcesByStatus.Count.ShouldBe(Enum.GetValues<ParseChannelSourceStatus>().Length);
		result.SourcesByStatus.Single(x => x.Status == ParseChannelSourceStatus.New).Count.ShouldBe(0);
	}

	[Fact]
	public async Task FillEveryDayOfPeriod_WhenRunsOnlyOnSomeDays()
	{
		var today = DateOnly.FromDateTime(Now.UtcDateTime);
		storage.Setup(s => s.GetRunsByDayAsync(userId, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync([new ParseChannelRunDailyDto(today.AddDays(-1), 5, 2)]);

		var result = await sut.Handle(new GetParseChannelStatsQuery(7), CancellationToken.None);

		result.PublishedByDay.Count.ShouldBe(7);
		result.SkippedByDay.Count.ShouldBe(7);
		result.PublishedByDay.Single(x => x.Date == today.AddDays(-1)).Count.ShouldBe(5);
		result.SkippedByDay.Single(x => x.Date == today.AddDays(-1)).Count.ShouldBe(2);
		result.PublishedByDay.Where(x => x.Date != today.AddDays(-1)).ShouldAllBe(x => x.Count == 0);
	}

	[Fact]
	public async Task QueryRunTotalsForRollingWindowsAndPeriod()
	{
		await sut.Handle(new GetParseChannelStatsQuery(14), CancellationToken.None);

		storage.Verify(s => s.GetRunTotalsAsync(userId, Now.AddHours(-24), It.IsAny<CancellationToken>()), Times.Once);
		storage.Verify(s => s.GetRunTotalsAsync(userId, Now.AddDays(-7), It.IsAny<CancellationToken>()), Times.Once);
		storage.Verify(s => s.GetRunTotalsAsync(userId, Now.AddDays(-30), It.IsAny<CancellationToken>()), Times.Once);
		storage.Verify(s => s.GetRunTotalsAsync(userId, new DateTimeOffset(2026, 10, 12, 0, 0, 0, TimeSpan.Zero),
			It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task QueryOnlyCurrentUserData()
	{
		await sut.Handle(new GetParseChannelStatsQuery(30), CancellationToken.None);

		storage.Verify(s => s.GetSourceStatusCountsAsync(It.Is<Guid>(x => x != userId),
			It.IsAny<CancellationToken>()), Times.Never);
		storage.Verify(s => s.GetPostTotalsAsync(It.Is<Guid>(x => x != userId), It.IsAny<CancellationToken>()),
			Times.Never);
		storage.Verify(s => s.GetRunTotalsAsync(It.Is<Guid>(x => x != userId), It.IsAny<DateTimeOffset>(),
			It.IsAny<CancellationToken>()), Times.Never);
		storage.Verify(s => s.GetTopSourcesAsync(userId, It.IsAny<DateTimeOffset>(), It.IsAny<int>(),
			It.IsAny<CancellationToken>()), Times.Once);
		storage.Verify(s => s.GetLastRunAtAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
	}

	private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
	{
		public override DateTimeOffset GetUtcNow() => now;
	}
}
