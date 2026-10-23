using Moq;
using Security.IdentityServices;
using Shared.Services;
using Shouldly;
using TgPoster.API.Domain.UseCases.Messages.ApproveMessages;
using TgPoster.Exceptions.BadRequest;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.Tests.Messages;

public sealed class ApproveMessagesUseCaseShould
{
	private static readonly TimeSpan OverdueMargin = TimeSpan.FromMinutes(2);

	private static readonly Dictionary<DayOfWeek, List<TimeOnly>> EveryDaySchedule =
		Enum.GetValues<DayOfWeek>().ToDictionary(x => x, _ => new List<TimeOnly> { new(10, 0), new(15, 0) });

	private readonly Mock<IApproveMessagesStorage> storage = new();
	private readonly ApproveMessagesUseCase sut;
	private readonly TimePostingService timePostingService = new();
	private readonly Guid userId = Guid.NewGuid();

	public ApproveMessagesUseCaseShould()
	{
		var identity = new Mock<IIdentityProvider>();
		identity.Setup(x => x.Current).Returns(new Identity(userId));

		sut = new ApproveMessagesUseCase(storage.Object, identity.Object, timePostingService);
	}

	[Fact]
	public async Task ThrowMessageNotFound_WhenMessageNotOwnedOrMissing()
	{
		var owned = Pending(Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(1));
		var foreignId = Guid.NewGuid();
		SetupMessages(owned);

		var exception = await Should.ThrowAsync<MessageNotFoundException>(async () =>
			await sut.Handle(new ApproveMessagesCommand([owned.Id, foreignId]), CancellationToken.None));

		exception.Message.ShouldContain(foreignId.ToString());
		storage.Verify(
			x => x.GetMessagesAsync(It.IsAny<List<Guid>>(), userId, It.IsAny<CancellationToken>()),
			Times.Once);
		VerifyNotApproved();
	}

	[Fact]
	public async Task ApproveWithoutTimeChange_WhenMessageInFuture()
	{
		var message = Pending(Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(1));
		SetupMessages(message);
		var timePostings = CaptureTimePostings();

		await sut.Handle(new ApproveMessagesCommand([message.Id, message.Id]), CancellationToken.None);

		timePostings.ShouldBeEmpty();
		storage.Verify(
			x => x.ApproveMessage(
				It.Is<List<Guid>>(ids => ids.SequenceEqual(new[] { message.Id })),
				userId,
				It.IsAny<IReadOnlyDictionary<Guid, DateTimeOffset>>(),
				It.IsAny<CancellationToken>()),
			Times.Once);
		VerifyScheduleNotRequested();
	}

	[Fact]
	public async Task RescheduleToFreeSlotOfItsSchedule_WhenTimePassed()
	{
		var scheduleId = Guid.NewGuid();
		var overdue = Pending(scheduleId, DateTimeOffset.UtcNow.AddHours(-1));
		SetupMessages(overdue);
		storage
			.Setup(x => x.GetScheduleTimeAsync(scheduleId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(EveryDaySchedule);
		DateTimeOffset? now = null;
		storage
			.Setup(x => x.GetOccupiedTimesAsync(scheduleId, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((Guid _, DateTimeOffset value, CancellationToken _) =>
			{
				now = value;
				return timePostingService.GetFreeSlots(1, EveryDaySchedule, [], value.Add(OverdueMargin));
			});
		var timePostings = CaptureTimePostings();
		var before = DateTimeOffset.UtcNow;

		await sut.Handle(new ApproveMessagesCommand([overdue.Id]), CancellationToken.None);

		var after = DateTimeOffset.UtcNow;
		now.ShouldNotBeNull();
		now.Value.ShouldBeInRange(before, after);
		var slots = timePostingService.GetFreeSlots(2, EveryDaySchedule, [], now.Value.Add(OverdueMargin));
		timePostings.Count.ShouldBe(1);
		timePostings[overdue.Id].ShouldBe(slots[1]);
	}

	[Fact]
	public async Task RescheduleToFreeSlot_WhenTimeWithinOverdueMargin()
	{
		var scheduleId = Guid.NewGuid();
		var soon = Pending(scheduleId, DateTimeOffset.UtcNow.AddMinutes(1));
		SetupMessages(soon);
		storage
			.Setup(x => x.GetScheduleTimeAsync(scheduleId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(EveryDaySchedule);
		DateTimeOffset? now = null;
		storage
			.Setup(x => x.GetOccupiedTimesAsync(scheduleId, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((Guid _, DateTimeOffset value, CancellationToken _) =>
			{
				now = value;
				return [];
			});
		var timePostings = CaptureTimePostings();

		await sut.Handle(new ApproveMessagesCommand([soon.Id]), CancellationToken.None);

		now.ShouldNotBeNull();
		var slots = timePostingService.GetFreeSlots(1, EveryDaySchedule, [], now.Value.Add(OverdueMargin));
		timePostings.Count.ShouldBe(1);
		timePostings[soon.Id].ShouldBe(slots[0]);
	}

	[Fact]
	public async Task RescheduleEachScheduleSeparately_WhenMessagesFromTwoSchedules()
	{
		var firstScheduleId = Guid.NewGuid();
		var secondScheduleId = Guid.NewGuid();
		var original = DateTimeOffset.UtcNow.AddDays(-1);
		var firstLate = Pending(firstScheduleId, original.AddHours(1));
		var firstEarly = Pending(firstScheduleId, original);
		var second = Pending(secondScheduleId, original);
		SetupMessages(firstLate, firstEarly, second);
		storage
			.Setup(x => x.GetScheduleTimeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(EveryDaySchedule);
		DateTimeOffset? now = null;
		storage
			.Setup(x => x.GetOccupiedTimesAsync(firstScheduleId, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((Guid _, DateTimeOffset value, CancellationToken _) =>
			{
				now = value;
				return timePostingService.GetFreeSlots(1, EveryDaySchedule, [], value.Add(OverdueMargin));
			});
		storage
			.Setup(x => x.GetOccupiedTimesAsync(secondScheduleId, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);
		var timePostings = CaptureTimePostings();

		await sut.Handle(
			new ApproveMessagesCommand([firstLate.Id, firstEarly.Id, second.Id]),
			CancellationToken.None);

		now.ShouldNotBeNull();
		var slots = timePostingService.GetFreeSlots(3, EveryDaySchedule, [], now.Value.Add(OverdueMargin));
		timePostings.Count.ShouldBe(3);
		timePostings[firstEarly.Id].ShouldBe(slots[1]);
		timePostings[firstLate.Id].ShouldBe(slots[2]);
		timePostings[second.Id].ShouldBe(slots[0]);
		storage.Verify(
			x => x.GetOccupiedTimesAsync(secondScheduleId, now.Value, It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task KeepTimePosting_WhenOverdueMessageAlreadyVerifiedOrNotRegistered()
	{
		var scheduleId = Guid.NewGuid();
		var past = DateTimeOffset.UtcNow.AddHours(-1);
		var verified = new ApproveMessageInfo(Guid.NewGuid(), scheduleId, true, true, past);
		var notRegistered = new ApproveMessageInfo(Guid.NewGuid(), scheduleId, false, false, past);
		SetupMessages(verified, notRegistered);
		var timePostings = CaptureTimePostings();

		await sut.Handle(new ApproveMessagesCommand([verified.Id, notRegistered.Id]), CancellationToken.None);

		timePostings.ShouldBeEmpty();
		storage.Verify(
			x => x.ApproveMessage(
				It.Is<List<Guid>>(ids => ids.SequenceEqual(new[] { verified.Id, notRegistered.Id })),
				userId,
				It.IsAny<IReadOnlyDictionary<Guid, DateTimeOffset>>(),
				It.IsAny<CancellationToken>()),
			Times.Once);
		VerifyScheduleNotRequested();
	}

	[Fact]
	public async Task ThrowNotFoundTime_WhenFreeSlotsFewerThanMessages()
	{
		var scheduleId = Guid.NewGuid();
		var messages = Enumerable.Range(0, 60)
			.Select(i => Pending(scheduleId, DateTimeOffset.UtcNow.AddMinutes(-i)))
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
				new ApproveMessagesCommand(messages.Select(x => x.Id).ToList()),
				CancellationToken.None));

		VerifyNotApproved();
	}

	private static ApproveMessageInfo Pending(Guid scheduleId, DateTimeOffset timePosting) =>
		new(Guid.NewGuid(), scheduleId, false, true, timePosting);

	private void SetupMessages(params ApproveMessageInfo[] messages)
	{
		storage
			.Setup(x => x.GetMessagesAsync(It.IsAny<List<Guid>>(), userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(messages.ToList());
	}

	private Dictionary<Guid, DateTimeOffset> CaptureTimePostings()
	{
		var captured = new Dictionary<Guid, DateTimeOffset>();
		storage
			.Setup(x => x.ApproveMessage(
				It.IsAny<List<Guid>>(),
				userId,
				It.IsAny<IReadOnlyDictionary<Guid, DateTimeOffset>>(),
				It.IsAny<CancellationToken>()))
			.Callback<List<Guid>, Guid, IReadOnlyDictionary<Guid, DateTimeOffset>, CancellationToken>(
				(_, _, timePostings, _) =>
				{
					foreach (var (id, timePosting) in timePostings)
					{
						captured[id] = timePosting;
					}
				})
			.Returns(Task.CompletedTask);
		return captured;
	}

	private void VerifyNotApproved()
	{
		storage.Verify(
			x => x.ApproveMessage(
				It.IsAny<List<Guid>>(),
				It.IsAny<Guid>(),
				It.IsAny<IReadOnlyDictionary<Guid, DateTimeOffset>>(),
				It.IsAny<CancellationToken>()),
			Times.Never);
	}

	private void VerifyScheduleNotRequested()
	{
		storage.Verify(
			x => x.GetScheduleTimeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
			Times.Never);
		storage.Verify(
			x => x.GetOccupiedTimesAsync(It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
			Times.Never);
	}
}
