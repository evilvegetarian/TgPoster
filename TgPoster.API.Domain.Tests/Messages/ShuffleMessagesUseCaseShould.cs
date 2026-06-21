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
		var slots = new List<MessageSlot>
		{
			new(Guid.NewGuid(), DateTimeOffset.UtcNow.AddMinutes(10)),
			new(Guid.NewGuid(), DateTimeOffset.UtcNow.AddMinutes(20)),
			new(Guid.NewGuid(), DateTimeOffset.UtcNow.AddMinutes(30))
		};
		storage
			.Setup(s => s.ExistAsync(scheduleId, userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);
		storage
			.Setup(s => s.GetMessagesAsync(scheduleId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(slots);

		List<Guid>? passedIds = null;
		List<DateTimeOffset>? passedTimes = null;
		storage
			.Setup(s => s.UpdateTimeAsync(It.IsAny<List<Guid>>(), It.IsAny<List<DateTimeOffset>>(),
				It.IsAny<CancellationToken>()))
			.Callback<List<Guid>, List<DateTimeOffset>, CancellationToken>((ids, times, _) =>
			{
				passedIds = ids;
				passedTimes = times;
			})
			.Returns(Task.CompletedTask);

		await sut.Handle(new ShuffleMessagesCommand(scheduleId), CancellationToken.None);

		passedIds.ShouldBe(slots.Select(x => x.Id).ToList());
		passedTimes.ShouldBe(slots.Select(x => x.TimePosting).ToList(), true);
	}
}
