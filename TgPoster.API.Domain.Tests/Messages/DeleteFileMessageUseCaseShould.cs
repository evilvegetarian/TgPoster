using Moq;
using Security.IdentityServices;
using Shouldly;
using TgPoster.API.Domain.UseCases.Messages.DeleteFileMessage;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.Tests.Messages;

public sealed class DeleteFileMessageUseCaseShould
{
	private readonly Mock<IDeleteFileMessageStorage> storage = new();
	private readonly DeleteFileMessageUseCase sut;
	private readonly Guid userId = Guid.NewGuid();

	public DeleteFileMessageUseCaseShould()
	{
		var identity = new Mock<IIdentityProvider>();
		identity.Setup(x => x.Current).Returns(new Identity(userId));
		sut = new DeleteFileMessageUseCase(storage.Object, identity.Object);
	}

	[Fact]
	public async Task DeleteFileOnlyWithinMessage_WhenMessageBelongsToUser()
	{
		var command = new DeleteFileMessageCommand(Guid.NewGuid(), Guid.NewGuid());
		storage.Setup(x => x.ExistMessageAsync(command.Id, userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);

		await sut.Handle(command, CancellationToken.None);

		storage.Verify(x => x.DeleteFileAsync(command.Id, command.FileId, It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task ThrowNotFound_WhenMessageBelongsToAnotherUser()
	{
		var command = new DeleteFileMessageCommand(Guid.NewGuid(), Guid.NewGuid());

		await Should.ThrowAsync<MessageNotFoundException>(() => sut.Handle(command, CancellationToken.None));

		storage.Verify(x => x.DeleteFileAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
			Times.Never);
	}
}
