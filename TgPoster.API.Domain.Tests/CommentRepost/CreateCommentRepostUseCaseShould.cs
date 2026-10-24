using Moq;
using Security.IdentityServices;
using Shouldly;
using TgPoster.API.Domain.UseCases.CommentRepost.CreateCommentRepost;
using TgPoster.Exceptions.NotFound;
using TgPoster.Telegram.Abstractions;

namespace TgPoster.API.Domain.Tests.CommentRepost;

public sealed class CreateCommentRepostUseCaseShould
{
	private readonly Mock<ITelegramChatService> chatService = new();
	private readonly Mock<ICreateCommentRepostStorage> storage = new();
	private readonly CreateCommentRepostUseCase sut;
	private readonly Guid userId = Guid.NewGuid();

	public CreateCommentRepostUseCaseShould()
	{
		var identity = new Mock<IIdentityProvider>();
		identity.Setup(x => x.Current).Returns(new Identity(userId));
		sut = new CreateCommentRepostUseCase(storage.Object, chatService.Object, identity.Object);
	}

	[Fact]
	public async Task ThrowNotFound_WhenTelegramSessionBelongsToAnotherUser()
	{
		var command = new CreateCommentRepostCommand(Guid.NewGuid(), Guid.NewGuid(), "@channel");
		storage.Setup(x => x.ScheduleExistsAsync(command.ScheduleId, userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);
		storage.Setup(x => x.TelegramSessionExistsAndActiveAsync(command.TelegramSessionId, userId,
				It.IsAny<CancellationToken>()))
			.ReturnsAsync(false);

		await Should.ThrowAsync<TelegramSessionEntityNotFoundException>(() =>
			sut.Handle(command, CancellationToken.None));

		chatService.VerifyNoOtherCalls();
	}

	[Fact]
	public async Task ThrowNotFound_WhenScheduleBelongsToAnotherUser()
	{
		var command = new CreateCommentRepostCommand(Guid.NewGuid(), Guid.NewGuid(), "@channel");

		await Should.ThrowAsync<ScheduleNotFoundException>(() => sut.Handle(command, CancellationToken.None));

		chatService.VerifyNoOtherCalls();
	}
}
