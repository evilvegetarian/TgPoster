using Moq;
using Security.IdentityServices;
using Shared.Services;
using Shouldly;
using TgPoster.API.Domain.UseCases.Messages.RetryMessages;
using TgPoster.Exceptions.BadRequest;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.Tests.Messages;

public sealed class RetryMessagesUseCaseShould
{
	private static readonly Dictionary<DayOfWeek, List<TimeOnly>> EveryDaySchedule =
		Enum.GetValues<DayOfWeek>().ToDictionary(x => x, _ => new List<TimeOnly> { new(10, 0), new(15, 0) });

	private readonly Mock<IRetryMessagesStorage> storage = new();
	private readonly RetryMessagesUseCase sut;
	private readonly TimePostingService timePostingService = new();
	private readonly Guid userId = Guid.NewGuid();

	public RetryMessagesUseCaseShould()
	{
		var identity = new Mock<IIdentityProvider>();
		identity.Setup(x => x.Current).Returns(new Identity(userId));

		sut = new RetryMessagesUseCase(storage.Object, identity.Object, timePostingService);
	}

	[Fact]
	public async Task RequestDistinctMessagesOfCurrentUser_WhenRetrying()
	{
		var message = Failed(Guid.NewGuid(), DateTimeOffset.UtcNow);
		SetupMessages(message);

		await sut.Handle(new RetryMessagesCommand([message.Id, message.Id], RetryMode.Now), CancellationToken.None);

		storage.Verify(
			x => x.GetMessagesAsync(
				It.Is<List<Guid>>(ids => ids.SequenceEqual(new[] { message.Id })),
				userId,
				It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task ThrowMessageNotFound_WhenMessageNotOwnedOrMissing()
	{
		var owned = Failed(Guid.NewGuid(), DateTimeOffset.UtcNow);
		var missingId = Guid.NewGuid();
		SetupMessages(owned);

		var exception = await Should.ThrowAsync<MessageNotFoundException>(async () =>
			await sut.Handle(new RetryMessagesCommand([owned.Id, missingId], RetryMode.Now), CancellationToken.None));

		exception.Message.ShouldContain(missingId.ToString());
		VerifyNotRescheduled();
	}

	[Fact]
	public async Task ThrowMessageNotFailed_WhenMessageNotInErrorStatus()
	{
		var failed = Failed(Guid.NewGuid(), DateTimeOffset.UtcNow);
		var sent = failed with { Id = Guid.NewGuid(), IsFailed = false };
		SetupMessages(failed, sent);

		var exception = await Should.ThrowAsync<MessageNotFailedException>(async () =>
			await sut.Handle(new RetryMessagesCommand([failed.Id, sent.Id], RetryMode.Now), CancellationToken.None));

		exception.Message.ShouldContain(sent.Id.ToString());
		VerifyNotRescheduled();
	}

	[Fact]
	public async Task RescheduleWithMinuteStepInOriginalOrder_WhenModeNow()
	{
		var scheduleId = Guid.NewGuid();
		var original = DateTimeOffset.UtcNow.AddDays(-1);
		var late = Failed(scheduleId, original.AddHours(2));
		var early = Failed(scheduleId, original);
		var middle = Failed(scheduleId, original.AddHours(1));
		SetupMessages(late, early, middle);
		var rescheduled = CaptureRescheduled();
		var before = DateTimeOffset.UtcNow;

		var result = await sut.Handle(
			new RetryMessagesCommand([late.Id, early.Id, middle.Id], RetryMode.Now),
			CancellationToken.None);

		var after = DateTimeOffset.UtcNow;
		result.Items.Select(x => x.Id).ShouldBe([early.Id, middle.Id, late.Id]);
		result.Items[0].TimePosting.ShouldBeInRange(before.AddMinutes(1), after.AddMinutes(1));
		result.Items[1].TimePosting.ShouldBe(result.Items[0].TimePosting.AddMinutes(1));
		result.Items[2].TimePosting.ShouldBe(result.Items[0].TimePosting.AddMinutes(2));
		rescheduled.ShouldBe(result.Items);
		storage.Verify(
			x => x.GetOccupiedTimesAsync(It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
			Times.Never);
	}

	[Fact]
	public async Task RescheduleToFreeSlotsOfEachSchedule_WhenModeNextFreeSlot()
	{
		var firstScheduleId = Guid.NewGuid();
		var secondScheduleId = Guid.NewGuid();
		var original = DateTimeOffset.UtcNow.AddDays(-1);
		var firstLate = Failed(firstScheduleId, original.AddHours(1));
		var firstEarly = Failed(firstScheduleId, original);
		var second = Failed(secondScheduleId, original);
		SetupMessages(firstLate, firstEarly, second);
		storage
			.Setup(x => x.GetScheduleTimeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(EveryDaySchedule);
		DateTimeOffset? from = null;
		storage
			.Setup(x => x.GetOccupiedTimesAsync(firstScheduleId, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((Guid _, DateTimeOffset value, CancellationToken _) =>
			{
				from = value;
				return timePostingService.GetFreeSlots(1, EveryDaySchedule, [], value);
			});
		storage
			.Setup(x => x.GetOccupiedTimesAsync(secondScheduleId, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);
		var rescheduled = CaptureRescheduled();

		var result = await sut.Handle(
			new RetryMessagesCommand([firstLate.Id, firstEarly.Id, second.Id], RetryMode.NextFreeSlot),
			CancellationToken.None);

		from.ShouldNotBeNull();
		var slots = timePostingService.GetFreeSlots(3, EveryDaySchedule, [], from.Value);
		result.Items.ShouldBe([
			new RetriedMessage { Id = firstEarly.Id, TimePosting = slots[1] },
			new RetriedMessage { Id = firstLate.Id, TimePosting = slots[2] },
			new RetriedMessage { Id = second.Id, TimePosting = slots[0] }
		]);
		rescheduled.ShouldBe(result.Items);
		storage.Verify(
			x => x.GetOccupiedTimesAsync(secondScheduleId, from.Value, It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task ThrowNotFoundTime_WhenFreeSlotsFewerThanMessages()
	{
		var scheduleId = Guid.NewGuid();
		var messages = Enumerable.Range(0, 60)
			.Select(i => Failed(scheduleId, DateTimeOffset.UtcNow.AddMinutes(-i)))
			.ToArray();
		SetupMessages(messages);
		storage
			.Setup(x => x.GetScheduleTimeAsync(scheduleId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new Dictionary<DayOfWeek, List<TimeOnly>> { { DayOfWeek.Monday, [new TimeOnly(10, 0)] } });
		storage
			.Setup(x => x.GetOccupiedTimesAsync(scheduleId, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);

		await Should.ThrowAsync<NotFoundTimeException>(async () =>
			await sut.Handle(
				new RetryMessagesCommand(messages.Select(x => x.Id).ToList(), RetryMode.NextFreeSlot),
				CancellationToken.None));

		VerifyNotRescheduled();
	}

	private static RetryMessageInfo Failed(Guid scheduleId, DateTimeOffset timePosting) =>
		new(Guid.NewGuid(), scheduleId, true, timePosting);

	private void SetupMessages(params RetryMessageInfo[] messages)
	{
		storage
			.Setup(x => x.GetMessagesAsync(It.IsAny<List<Guid>>(), userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(messages.ToList());
	}

	private List<RetriedMessage> CaptureRescheduled()
	{
		var rescheduled = new List<RetriedMessage>();
		storage
			.Setup(x => x.RescheduleAsync(It.IsAny<IReadOnlyCollection<RetriedMessage>>(), It.IsAny<CancellationToken>()))
			.Callback<IReadOnlyCollection<RetriedMessage>, CancellationToken>((items, _) => rescheduled.AddRange(items))
			.Returns(Task.CompletedTask);
		return rescheduled;
	}

	private void VerifyNotRescheduled()
	{
		storage.Verify(
			x => x.RescheduleAsync(It.IsAny<IReadOnlyCollection<RetriedMessage>>(), It.IsAny<CancellationToken>()),
			Times.Never);
	}
}
