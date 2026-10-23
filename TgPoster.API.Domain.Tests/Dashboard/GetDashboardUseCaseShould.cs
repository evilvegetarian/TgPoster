using Moq;
using Security.IdentityServices;
using Shouldly;
using TgPoster.API.Domain.UseCases.Dashboard.GetDashboard;
using TgPoster.Telegram.Models;

namespace TgPoster.API.Domain.Tests.Dashboard;

public sealed class GetDashboardUseCaseShould
{
	private static readonly DateTimeOffset Now = new(2026, 10, 24, 12, 0, 0, TimeSpan.Zero);

	private readonly Mock<IGetDashboardStorage> storage = new();
	private readonly GetDashboardUseCase sut;
	private readonly Guid userId = Guid.NewGuid();

	public GetDashboardUseCaseShould()
	{
		var identity = new Mock<IIdentityProvider>();
		identity.Setup(x => x.Current).Returns(new Identity(userId));

		storage.Setup(x => x.GetSchedulesAsync(It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);
		storage.Setup(x => x.GetUpcomingPostsAsync(It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<int>(),
				It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);
		storage.Setup(x => x.GetFailedPostsAsync(It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<int>(),
				It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);
		storage.Setup(x => x.GetRepostCountsAsync(It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(),
				It.IsAny<CancellationToken>()))
			.ReturnsAsync(new DashboardRepostCounts(0, 0, 0));
		storage.Setup(x => x.GetRepostTopErrorsAsync(It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<int>(),
				It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);
		storage.Setup(x => x.GetSessionsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);
		storage.Setup(x => x.GetParsingProblemsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);
		storage.Setup(x => x.GetWorkersAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);

		sut = new GetDashboardUseCase(storage.Object, identity.Object, new FixedTimeProvider(Now));
	}

	[Fact]
	public async Task RoundQueueDaysToOneDecimal_WhenLastPlannedAtInFuture()
	{
		SetupSchedule(lastPlannedAt: Now.AddDays(3).AddHours(5));

		var result = await sut.Handle(new GetDashboardQuery(), CancellationToken.None);

		result.Schedules.Single().QueueDays.ShouldBe(3.2);
	}

	[Fact]
	public async Task ReturnNullQueueDays_WhenLastPlannedAtIsNull()
	{
		SetupSchedule(lastPlannedAt: null);

		var result = await sut.Handle(new GetDashboardQuery(), CancellationToken.None);

		result.Schedules.Single().QueueDays.ShouldBeNull();
	}

	[Fact]
	public async Task ReturnZeroQueueDays_WhenLastPlannedAtInPast()
	{
		SetupSchedule(lastPlannedAt: Now.AddHours(-2));

		var result = await sut.Handle(new GetDashboardQuery(), CancellationToken.None);

		result.Schedules.Single().QueueDays.ShouldBe(0);
	}

	[Theory]
	[InlineData("+79161234567", "•••4567")]
	[InlineData("7 (916) 123-45-67", "•••4567")]
	[InlineData("12345", "•••2345")]
	[InlineData("1234", "•••")]
	[InlineData("12", "•••")]
	[InlineData("", "•••")]
	public async Task MaskPhone_WhenReturningSessions(string phone, string expected)
	{
		SetupSession(phone: phone);

		var result = await sut.Handle(new GetDashboardQuery(), CancellationToken.None);

		result.TelegramSessions.Single().PhoneMasked.ShouldBe(expected);
	}

	[Fact]
	public async Task NotMarkProblem_WhenSessionIsActiveAuthorizedWithoutFloodWait()
	{
		SetupSession();

		var result = await sut.Handle(new GetDashboardQuery(), CancellationToken.None);

		result.TelegramSessions.Single().HasProblem.ShouldBeFalse();
	}

	[Fact]
	public async Task NotMarkProblem_WhenFloodWaitAlreadyExpired()
	{
		SetupSession(floodWaitUntil: Now.AddMinutes(-1));

		var result = await sut.Handle(new GetDashboardQuery(), CancellationToken.None);

		result.TelegramSessions.Single().HasProblem.ShouldBeFalse();
	}

	[Fact]
	public async Task MarkProblem_WhenSessionIsInactive()
	{
		SetupSession(isActive: false);

		var result = await sut.Handle(new GetDashboardQuery(), CancellationToken.None);

		result.TelegramSessions.Single().HasProblem.ShouldBeTrue();
	}

	[Fact]
	public async Task MarkProblem_WhenSessionIsNotAuthorized()
	{
		SetupSession(status: TelegramSessionStatus.Failed);

		var result = await sut.Handle(new GetDashboardQuery(), CancellationToken.None);

		result.TelegramSessions.Single().HasProblem.ShouldBeTrue();
	}

	[Fact]
	public async Task MarkProblem_WhenFloodWaitIsInFuture()
	{
		SetupSession(floodWaitUntil: Now.AddMinutes(10));

		var result = await sut.Handle(new GetDashboardQuery(), CancellationToken.None);

		result.TelegramSessions.Single().HasProblem.ShouldBeTrue();
	}

	[Fact]
	public async Task RequestStorageWithCurrentUserAndDayWindow()
	{
		await sut.Handle(new GetDashboardQuery(), CancellationToken.None);

		var since = Now.AddHours(-24);
		storage.Verify(x => x.GetSchedulesAsync(userId, Now, It.IsAny<CancellationToken>()), Times.Once);
		storage.Verify(x => x.GetUpcomingPostsAsync(userId, Now, 10, It.IsAny<CancellationToken>()), Times.Once);
		storage.Verify(x => x.GetFailedPostsAsync(userId, since, 20, It.IsAny<CancellationToken>()), Times.Once);
		storage.Verify(x => x.GetFailedPostsCountAsync(userId, since, It.IsAny<CancellationToken>()), Times.Once);
		storage.Verify(x => x.GetRepostCountsAsync(userId, since, It.IsAny<CancellationToken>()), Times.Once);
		storage.Verify(x => x.GetRepostTopErrorsAsync(userId, since, 3, It.IsAny<CancellationToken>()), Times.Once);
		storage.Verify(x => x.GetSessionsAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
		storage.Verify(x => x.GetParsingProblemsAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
	}

	private void SetupSchedule(DateTimeOffset? lastPlannedAt)
	{
		var row = new DashboardScheduleRow(Guid.NewGuid(), "Расписание", "channel", true, true, 1, 0, null,
			lastPlannedAt, 0);
		storage.Setup(x => x.GetSchedulesAsync(userId, Now, It.IsAny<CancellationToken>()))
			.ReturnsAsync([row]);
	}

	private void SetupSession(
		string phone = "+79161234567",
		bool isActive = true,
		TelegramSessionStatus status = TelegramSessionStatus.Authorized,
		DateTimeOffset? floodWaitUntil = null
	)
	{
		var row = new DashboardSessionRow(Guid.NewGuid(), "Аккаунт", phone, status, isActive, floodWaitUntil);
		storage.Setup(x => x.GetSessionsAsync(userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync([row]);
	}

	private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
	{
		public override DateTimeOffset GetUtcNow() => now;
	}
}
