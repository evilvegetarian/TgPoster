using MassTransit;
using Moq;
using Security.IdentityServices;
using Shared.Enums;
using Shared.Telegram;
using Shouldly;
using TgPoster.API.Domain.UseCases.ChannelCleanup.StartChannelCleanup;
using TgPoster.Exceptions.BadRequest;
using TgPoster.Exceptions.NotFound;
using TgPoster.Telegram.Abstractions;
using TgPoster.Telegram.Models;

namespace TgPoster.API.Domain.Tests.ChannelCleanup;

public sealed class StartChannelCleanupUseCaseShould
{
	private readonly Mock<IBus> bus;
	private readonly Guid jobId = Guid.NewGuid();
	private readonly Mock<ITelegramMessageService> messageService;
	private readonly Guid sessionId = Guid.NewGuid();
	private readonly Mock<IStartChannelCleanupStorage> storage;
	private readonly StartChannelCleanupUseCase sut;
	private readonly Guid userId = Guid.NewGuid();
	private IReadOnlyList<ChannelCleanupItemDraft>? createdItems;

	public StartChannelCleanupUseCaseShould()
	{
		storage = new Mock<IStartChannelCleanupStorage>();
		messageService = new Mock<ITelegramMessageService>();
		bus = new Mock<IBus>();
		var identityProvider = new Mock<IIdentityProvider>();
		identityProvider.Setup(x => x.Current).Returns(new Identity(userId));

		storage.Setup(s => s.SessionExistsAsync(sessionId, userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);
		storage.Setup(s => s.GetUsedChatsAsync(sessionId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new UsedTelegramChats([], []));
		storage.Setup(s => s.CreateJobAsync(
				sessionId, It.IsAny<IReadOnlyList<ChannelCleanupItemDraft>>(), It.IsAny<CancellationToken>()))
			.Callback<Guid, IReadOnlyList<ChannelCleanupItemDraft>, CancellationToken>((_, items, _) =>
				createdItems = items)
			.ReturnsAsync(jobId);

		sut = new StartChannelCleanupUseCase(
			storage.Object, messageService.Object, bus.Object, identityProvider.Object);
	}

	[Fact]
	public async Task CreateJobForConfirmedCandidates_AndPublishIt()
	{
		SetupChats(
			ChannelCleanupTestChats.Channel(1, "Первый"),
			ChannelCleanupTestChats.BasicGroup(2, "Группа"),
			ChannelCleanupTestChats.Channel(3, "Не подтверждён"));

		var result = await sut.Handle(new StartChannelCleanupCommand(sessionId, [1, 2]), CancellationToken.None);

		result.JobId.ShouldBe(jobId);
		result.Status.ShouldBe(ChannelCleanupStatus.Pending);
		result.PendingCount.ShouldBe(2);
		createdItems.ShouldNotBeNull();
		createdItems.Select(x => x.ChatId).ShouldBe([1, 2], ignoreOrder: true);
		var channel = createdItems.Single(x => x.ChatId == 1);
		channel.AccessHash.ShouldBe(10);
		channel.IsBasicGroup.ShouldBeFalse();
		createdItems.Single(x => x.ChatId == 2).IsBasicGroup.ShouldBeTrue();
		bus.Verify(b => b.Publish(
			It.Is<CleanupChannelsContract>(x => x.JobId == jobId), It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task SkipConfirmedChannel_ThatBecameUsedOrAdministered()
	{
		SetupChats(
			ChannelCleanupTestChats.Channel(1, "Стал направлением"),
			ChannelCleanupTestChats.Channel(2, "Стал админом", isAdmin: true),
			ChannelCleanupTestChats.Channel(3, "Мусор"));
		storage.Setup(s => s.GetUsedChatsAsync(sessionId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new UsedTelegramChats([1], []));

		await sut.Handle(new StartChannelCleanupCommand(sessionId, [1, 2, 3]), CancellationToken.None);

		createdItems.ShouldNotBeNull();
		createdItems.Select(x => x.ChatId).ShouldBe([3]);
	}

	[Fact]
	public async Task ThrowAndNotCreateJob_WhenNothingLeftToLeave()
	{
		SetupChats(ChannelCleanupTestChats.Channel(1, "Свой", isCreator: true));

		await Should.ThrowAsync<ChannelCleanupException>(async () =>
			await sut.Handle(new StartChannelCleanupCommand(sessionId, [1, 999]), CancellationToken.None));
		VerifyNoJobCreated();
	}

	[Fact]
	public async Task Throw_WhenNothingSelected()
	{
		await Should.ThrowAsync<ChannelCleanupException>(async () =>
			await sut.Handle(new StartChannelCleanupCommand(sessionId, []), CancellationToken.None));
		VerifyNoJobCreated();
	}

	[Fact]
	public async Task Throw_WhenCleanupAlreadyRunsForSession()
	{
		storage.Setup(s => s.GetActiveJobIdAsync(sessionId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Guid.NewGuid());

		await Should.ThrowAsync<ChannelCleanupException>(async () =>
			await sut.Handle(new StartChannelCleanupCommand(sessionId, [1]), CancellationToken.None));
		messageService.Verify(m => m.GetJoinedChatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
			Times.Never);
	}

	[Fact]
	public async Task ThrowNotFound_WhenSessionIsForeign()
	{
		storage.Setup(s => s.SessionExistsAsync(sessionId, userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(false);

		await Should.ThrowAsync<TelegramSessionEntityNotFoundException>(async () =>
			await sut.Handle(new StartChannelCleanupCommand(sessionId, [1]), CancellationToken.None));
		VerifyNoJobCreated();
	}

	[Fact]
	public async Task Throw_WhenTelegramDoesNotReturnChats()
	{
		messageService.Setup(m => m.GetJoinedChatsAsync(sessionId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(TelegramOperationResult<IReadOnlyList<TelegramChatInfo>>.Failed(
				TelegramOperationStatus.SessionNotFound, "AUTH_KEY_UNREGISTERED"));

		await Should.ThrowAsync<ChannelCleanupException>(async () =>
			await sut.Handle(new StartChannelCleanupCommand(sessionId, [1]), CancellationToken.None));
		VerifyNoJobCreated();
	}

	private void SetupChats(params TelegramChatInfo[] chats) =>
		messageService.Setup(m => m.GetJoinedChatsAsync(sessionId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(TelegramOperationResult<IReadOnlyList<TelegramChatInfo>>.Success(chats));

	private void VerifyNoJobCreated() =>
		storage.Verify(s => s.CreateJobAsync(
			It.IsAny<Guid>(), It.IsAny<IReadOnlyList<ChannelCleanupItemDraft>>(), It.IsAny<CancellationToken>()),
			Times.Never);
}
