using Moq;
using Security.IdentityServices;
using Shouldly;
using TgPoster.API.Domain.UseCases.Parse.RefreshParseChannelInfo;
using TgPoster.Exceptions.NotFound;
using TgPoster.Telegram.Abstractions;

namespace TgPoster.API.Domain.Tests.Parse;

public sealed class RefreshParseChannelInfoUseCaseShould
{
	private readonly Mock<ITelegramChatService> chatService = new();
	private readonly Mock<IRefreshParseChannelInfoStorage> storage = new();
	private readonly RefreshParseChannelInfoUseCase sut;
	private readonly Guid userId = Guid.NewGuid();

	public RefreshParseChannelInfoUseCaseShould()
	{
		var identity = new Mock<IIdentityProvider>();
		identity.Setup(x => x.Current).Returns(new Identity(userId));
		sut = new RefreshParseChannelInfoUseCase(storage.Object, chatService.Object, identity.Object);
	}

	[Fact]
	public async Task UpdateMessagesCount_WhenSourceBelongsToUser()
	{
		var id = Guid.NewGuid();
		var sessionId = Guid.NewGuid();
		storage.Setup(x => x.GetParseChannelInfoAsync(id, userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync((sessionId, "source_channel"));
		chatService.Setup(x => x.GetChannelMessagesCountAsync(sessionId, "source_channel")).ReturnsAsync(42);

		await sut.Handle(new RefreshParseChannelInfoCommand(id), CancellationToken.None);

		storage.Verify(x => x.UpdateTotalMessagesCountAsync(id, 42, It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task ThrowNotFound_WhenSourceBelongsToAnotherUser()
	{
		var id = Guid.NewGuid();

		await Should.ThrowAsync<ParseChannelNotFoundException>(() =>
			sut.Handle(new RefreshParseChannelInfoCommand(id), CancellationToken.None));

		chatService.VerifyNoOtherCalls();
		storage.Verify(x => x.UpdateTotalMessagesCountAsync(It.IsAny<Guid>(), It.IsAny<int?>(),
			It.IsAny<CancellationToken>()), Times.Never);
	}
}
