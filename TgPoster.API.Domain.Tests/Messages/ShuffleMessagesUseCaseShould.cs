using Moq;
using Security.IdentityServices;
using Shouldly;
using TgPoster.API.Domain.UseCases.Messages.ShuffleMessages;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.Tests.Messages;

public class ShuffleMessagesUseCaseShould
{
	private readonly Mock<IShuffleMessagesStorage> storage;
	private readonly ShuffleMessagesUseCase sut;
	private readonly Guid userId = Guid.NewGuid();

	public ShuffleMessagesUseCaseShould()
	{
		storage = new Mock<IShuffleMessagesStorage>();
		var identityProvider = new Mock<IIdentityProvider>();

		var identity = new Identity(userId);
		identityProvider.Setup(x => x.Current).Returns(identity);

		sut = new ShuffleMessagesUseCase(storage.Object, identityProvider.Object);
	}

	[Fact]
	public async Task ThrowScheduleNotFoundException_WhenScheduleNotExists()
	{
		var scheduleId = Guid.NewGuid();
		storage
			.Setup(s => s.ExistAsync(scheduleId, userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(false);

		await Should.ThrowAsync<ScheduleNotFoundException>(async () =>
			await sut.Handle(new ShuffleMessagesCommand(scheduleId), CancellationToken.None));

		storage.Verify(
			s => s.UpdateTimeAsync(It.IsAny<List<Guid>>(), It.IsAny<List<DateTimeOffset>>(),
				It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task NotUpdate_WhenNoMessages()
	{
		var scheduleId = Guid.NewGuid();
		storage
			.Setup(s => s.ExistAsync(scheduleId, userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);
		storage
			.Setup(s => s.GetMessagesAsync(scheduleId, It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);

		await sut.Handle(new ShuffleMessagesCommand(scheduleId), CancellationToken.None);

		storage.Verify(
			s => s.UpdateTimeAsync(It.IsAny<List<Guid>>(), It.IsAny<List<DateTimeOffset>>(),
				It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task UpdateWithSameTimeSet_WhenMessagesExist()
	{
		var scheduleId = Guid.NewGuid();
		var now = DateTimeOffset.UtcNow;
		var slots = new List<MessageSlot>
		{
			Slot(now.AddMinutes(10)),
			Slot(now.AddMinutes(20), hasPhoto: true),
			Slot(now.AddMinutes(30), hasVideo: true)
		};
		SetupMessages(scheduleId, slots);
		var passed = CaptureUpdate();

		await sut.Handle(new ShuffleMessagesCommand(scheduleId), CancellationToken.None);

		passed.Ids.ShouldBe(slots.Select(x => x.Id), true);
		passed.Times.ShouldBe(slots.Select(x => x.TimePosting).Order());
	}

	[Fact]
	public async Task NotPutThreeOfSameKindOrSourceInRow_WhenQueueIsMixed()
	{
		var scheduleId = Guid.NewGuid();
		var sourceA = Guid.NewGuid();
		var sourceB = Guid.NewGuid();
		var now = DateTimeOffset.UtcNow;
		var slots = new List<MessageSlot>
		{
			Slot(now.AddMinutes(10), hasVideo: true, hasPhoto: true, source: sourceA),
			Slot(now.AddMinutes(20), hasVideo: true, source: sourceA),
			Slot(now.AddMinutes(30), hasVideo: true, source: sourceA),
			Slot(now.AddMinutes(40), hasVideo: true, source: sourceB),
			Slot(now.AddMinutes(50), hasPhoto: true, source: sourceB),
			Slot(now.AddMinutes(60), hasPhoto: true, source: sourceB),
			Slot(now.AddMinutes(70), hasPhoto: true, source: sourceB),
			Slot(now.AddMinutes(80), hasPhoto: true),
			Slot(now.AddMinutes(90)),
			Slot(now.AddMinutes(100)),
			Slot(now.AddMinutes(110)),
			Slot(now.AddMinutes(120), source: sourceA)
		};
		SetupMessages(scheduleId, slots);
		var passed = CaptureUpdate();

		await sut.Handle(new ShuffleMessagesCommand(scheduleId), CancellationToken.None);

		passed.Times.ShouldBe(slots.Select(x => x.TimePosting));
		var byId = slots.ToDictionary(x => x.Id);
		var queue = passed.Ids.Select(id => byId[id]).ToList();
		queue.Count.ShouldBe(slots.Count);
		for (var i = 2; i < queue.Count; i++)
		{
			var window = queue.GetRange(i - 2, 3);
			window.DistinctBy(Kind).Count().ShouldBeGreaterThan(1);
			window.DistinctBy(x => x.ChannelParsingSettingId).Count().ShouldBeGreaterThan(1);
		}
	}

	private void SetupMessages(Guid scheduleId, List<MessageSlot> slots)
	{
		storage
			.Setup(s => s.ExistAsync(scheduleId, userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);
		storage
			.Setup(s => s.GetMessagesAsync(scheduleId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(slots);
	}

	private PassedUpdate CaptureUpdate()
	{
		var passed = new PassedUpdate();
		storage
			.Setup(s => s.UpdateTimeAsync(It.IsAny<List<Guid>>(), It.IsAny<List<DateTimeOffset>>(),
				It.IsAny<CancellationToken>()))
			.Callback<List<Guid>, List<DateTimeOffset>, CancellationToken>((ids, times, _) =>
			{
				passed.Ids = ids;
				passed.Times = times;
			})
			.Returns(Task.CompletedTask);
		return passed;
	}

	private static MessageSlot Slot(
		DateTimeOffset time,
		bool hasVideo = false,
		bool hasPhoto = false,
		Guid? source = null
	)
	{
		return new MessageSlot(Guid.NewGuid(), time, hasVideo, hasPhoto, source);
	}

	private static string Kind(MessageSlot slot)
	{
		return slot.HasVideo ? "video" : slot.HasPhoto ? "photo" : "text";
	}

	private sealed class PassedUpdate
	{
		public List<Guid> Ids { get; set; } = [];

		public List<DateTimeOffset> Times { get; set; } = [];
	}
}
