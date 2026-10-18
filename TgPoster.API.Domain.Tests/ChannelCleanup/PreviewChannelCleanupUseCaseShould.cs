using Moq;
using Security.IdentityServices;
using Shared.Enums;
using Shouldly;
using TgPoster.API.Domain.UseCases.ChannelCleanup.PreviewChannelCleanup;
using TgPoster.Exceptions.BadRequest;
using TgPoster.Exceptions.NotFound;
using TgPoster.Telegram.Abstractions;
using TgPoster.Telegram.Models;

namespace TgPoster.API.Domain.Tests.ChannelCleanup;

public sealed class PreviewChannelCleanupUseCaseShould
{
	private readonly Mock<ITelegramMessageService> messageService;
	private readonly Guid sessionId = Guid.NewGuid();
	private readonly Mock<IPreviewChannelCleanupStorage> storage;
	private readonly PreviewChannelCleanupUseCase sut;
	private readonly Guid userId = Guid.NewGuid();

	public PreviewChannelCleanupUseCaseShould()
	{
		storage = new Mock<IPreviewChannelCleanupStorage>();
		messageService = new Mock<ITelegramMessageService>();
		var identityProvider = new Mock<IIdentityProvider>();
		identityProvider.Setup(x => x.Current).Returns(new Identity(userId));

		storage.Setup(s => s.SessionExistsAsync(sessionId, userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);
		storage.Setup(s => s.GetUsedChatsAsync(sessionId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new UsedTelegramChats([], []));

		sut = new PreviewChannelCleanupUseCase(storage.Object, messageService.Object, identityProvider.Object);
	}

	[Fact]
	public async Task KeepChannelsUsedByApp_AndOwnedOrAdministered()
	{
		SetupChats(
			ChannelCleanupTestChats.Channel(1, "Направление"),
			ChannelCleanupTestChats.Channel(2, "Парсинг", "parsechan"),
			ChannelCleanupTestChats.Channel(3, "Свой", isCreator: true),
			ChannelCleanupTestChats.Channel(4, "Админ", isAdmin: true),
			ChannelCleanupTestChats.Channel(5, "Мусор"),
			ChannelCleanupTestChats.BasicGroup(6, "Старая группа"));
		storage.Setup(s => s.GetUsedChatsAsync(sessionId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new UsedTelegramChats([-1000000000001], ["@parsechan"]));

		var result = await sut.Handle(new PreviewChannelCleanupQuery(sessionId), CancellationToken.None);

		result.TotalCount.ShouldBe(6);
		result.InUseCount.ShouldBe(2);
		result.AdminCount.ShouldBe(2);
		result.Candidates.Select(x => x.ChatId).ShouldBe([5, 6], ignoreOrder: true);
		result.Candidates.Single(x => x.ChatId == 6).ChatType.ShouldBe(ChatType.Group);
		result.Candidates.Single(x => x.ChatId == 5).ChatType.ShouldBe(ChatType.Channel);
	}

	[Fact]
	public async Task SortCandidatesByTitle()
	{
		SetupChats(
			ChannelCleanupTestChats.Channel(1, "Яблоко"),
			ChannelCleanupTestChats.Channel(2, "арбуз"),
			ChannelCleanupTestChats.Channel(3, "Банан"));

		var result = await sut.Handle(new PreviewChannelCleanupQuery(sessionId), CancellationToken.None);

		result.Candidates.Select(x => x.Title).ShouldBe(["арбуз", "Банан", "Яблоко"]);
	}

	[Fact]
	public async Task ThrowNotFound_WhenSessionIsForeign()
	{
		storage.Setup(s => s.SessionExistsAsync(sessionId, userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(false);

		await Should.ThrowAsync<TelegramSessionEntityNotFoundException>(async () =>
			await sut.Handle(new PreviewChannelCleanupQuery(sessionId), CancellationToken.None));
		messageService.Verify(m => m.GetJoinedChatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>(), It.IsAny<bool>()),
			Times.Never);
	}

	[Fact]
	public async Task ThrowReadableError_WhenTelegramRateLimits()
	{
		messageService.Setup(m => m.GetJoinedChatsAsync(sessionId, It.IsAny<CancellationToken>(), It.IsAny<bool>()))
			.ReturnsAsync(TelegramOperationResult<IReadOnlyList<TelegramChatInfo>>.Failed(
				TelegramOperationStatus.FloodWait, "FLOOD_WAIT_X", 300));

		var exception = await Should.ThrowAsync<ChannelCleanupException>(async () =>
			await sut.Handle(new PreviewChannelCleanupQuery(sessionId), CancellationToken.None));

		exception.Message.ShouldContain("5 мин");
	}

	private void SetupChats(params TelegramChatInfo[] chats) =>
		messageService.Setup(m => m.GetJoinedChatsAsync(sessionId, It.IsAny<CancellationToken>(), It.IsAny<bool>()))
			.ReturnsAsync(TelegramOperationResult<IReadOnlyList<TelegramChatInfo>>.Success(chats));
}
