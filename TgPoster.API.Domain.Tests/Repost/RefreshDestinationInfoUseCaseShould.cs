using Moq;
using Shared.Enums;
using Shouldly;
using TgPoster.API.Domain.UseCases.Repost.RefreshDestinationInfo;
using TgPoster.Exceptions.NotFound;
using TgPoster.Telegram.Abstractions;
using TgPoster.Telegram.Models;

namespace TgPoster.API.Domain.Tests.Repost;

public sealed class RefreshDestinationInfoUseCaseShould
{
	private const long ChatId = 1375846471;

	private readonly Mock<ITelegramChatService> chatService = new();
	private readonly Guid destinationId = Guid.NewGuid();
	private readonly Guid sessionId = Guid.NewGuid();
	private readonly Mock<IRefreshDestinationInfoStorage> storage = new();
	private readonly RefreshDestinationInfoUseCase sut;

	public RefreshDestinationInfoUseCaseShould()
	{
		storage.Setup(s => s.GetTelegramSessionIdAsync(destinationId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(sessionId);
		storage.Setup(s => s.GetChatIdAsync(destinationId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(ChatId);

		sut = new RefreshDestinationInfoUseCase(storage.Object, chatService.Object);
	}

	[Fact]
	public async Task RecordBanOnlyInRegistry_WhenChatIsForbiddenForAccount()
	{
		SetupRefresh(ChatStatus.Banned);

		await sut.Handle(new RefreshDestinationInfoCommand(destinationId), CancellationToken.None);

		storage.Verify(s => s.AddAccountBanAsync(
			sessionId, ChatId, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
		storage.Verify(s => s.UpdateDestinationInfoAsync(
			destinationId, It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<ChatType>(),
			null, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
	}

	[Theory]
	[InlineData(ChatStatus.Active)]
	[InlineData(ChatStatus.Left)]
	public async Task StoreMembershipStatus_WhenChatIsNotForbidden(ChatStatus status)
	{
		SetupRefresh(status);

		await sut.Handle(new RefreshDestinationInfoCommand(destinationId), CancellationToken.None);

		storage.Verify(s => s.UpdateDestinationInfoAsync(
			destinationId, It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<ChatType>(),
			status, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
	}

	[Theory]
	[InlineData(ChatStatus.Active)]
	[InlineData(ChatStatus.Left)]
	public async Task NotRecordAccountBan_WhenChatIsNotForbidden(ChatStatus status)
	{
		SetupRefresh(status);

		await sut.Handle(new RefreshDestinationInfoCommand(destinationId), CancellationToken.None);

		storage.Verify(s => s.AddAccountBanAsync(
			It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task ThrowNotFoundWithoutTelegram_WhenDestinationMissing()
	{
		storage.Setup(s => s.GetTelegramSessionIdAsync(destinationId, It.IsAny<CancellationToken>()))
			.ReturnsAsync((Guid?)null);

		await Should.ThrowAsync<RepostDestinationNotFoundException>(async () =>
			await sut.Handle(new RefreshDestinationInfoCommand(destinationId), CancellationToken.None));

		chatService.VerifyNoOtherCalls();
	}

	private void SetupRefresh(ChatStatus status) =>
		chatService.Setup(c => c.RefreshChannelInfoAsync(sessionId, ChatId))
			.ReturnsAsync(new TelegramChannelRefreshResult
			{
				Title = "Канал",
				ChatType = ChatType.Channel,
				ChatStatus = status
			});
}
