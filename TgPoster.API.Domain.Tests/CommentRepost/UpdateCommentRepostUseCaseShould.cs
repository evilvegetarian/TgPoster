using Moq;
using Security.IdentityServices;
using Shouldly;
using TgPoster.API.Domain.UseCases.CommentRepost.UpdateCommentRepost;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.Tests.CommentRepost;

public sealed class UpdateCommentRepostUseCaseShould
{
	private readonly Guid id = Guid.NewGuid();
	private readonly Mock<IUpdateCommentRepostStorage> storage = new();
	private readonly UpdateCommentRepostUseCase sut;
	private readonly Guid userId = Guid.NewGuid();

	public UpdateCommentRepostUseCaseShould()
	{
		var identity = new Mock<IIdentityProvider>();
		identity.Setup(x => x.Current).Returns(new Identity(userId));

		storage.Setup(s => s.ExistsAsync(id, userId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

		sut = new UpdateCommentRepostUseCase(storage.Object, identity.Object);
	}

	[Fact]
	public async Task RemoveAccountBanAndEnable_WhenEnabling()
	{
		await sut.Handle(new UpdateCommentRepostCommand(id, true), CancellationToken.None);

		storage.Verify(s => s.RemoveAccountBanAsync(id, It.IsAny<CancellationToken>()), Times.Once);
		storage.Verify(s => s.UpdateAsync(id, true, It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task KeepAccountBan_WhenDisabling()
	{
		await sut.Handle(new UpdateCommentRepostCommand(id, false), CancellationToken.None);

		storage.Verify(s => s.RemoveAccountBanAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
		storage.Verify(s => s.UpdateAsync(id, false, It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task ThrowNotFoundAndChangeNothing_WhenSettingsNotOwnedByUser()
	{
		storage.Setup(s => s.ExistsAsync(id, userId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

		await Should.ThrowAsync<CommentRepostSettingsNotFoundException>(async () =>
			await sut.Handle(new UpdateCommentRepostCommand(id, true), CancellationToken.None));

		storage.Verify(s => s.RemoveAccountBanAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
		storage.Verify(s => s.UpdateAsync(
			It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
	}
}
