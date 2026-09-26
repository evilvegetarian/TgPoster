using Moq;
using Security.IdentityServices;
using Shared.Enums;
using Shouldly;
using TgPoster.API.Domain.UseCases.Messages.RetryCrossPost;
using TgPoster.Exceptions.BadRequest;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.Tests.Messages;

public class RetryCrossPostUseCaseShould
{
	private readonly Guid crossPostId = Guid.NewGuid();
	private readonly Guid messageId = Guid.NewGuid();
	private readonly Mock<IRetryCrossPostStorage> storage = new();
	private readonly RetryCrossPostUseCase sut;
	private readonly Guid userId = Guid.NewGuid();

	public RetryCrossPostUseCaseShould()
	{
		var identity = new Mock<IIdentityProvider>();
		identity.Setup(x => x.Current).Returns(new Identity(userId));

		storage.Setup(x => x.GetRetryInfoAsync(messageId, crossPostId, userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new CrossPostRetryInfo { Status = CrossPostStatus.Failed, AccountActive = true });

		sut = new RetryCrossPostUseCase(storage.Object, identity.Object);
	}

	private static RetryCrossPostCommand Command(Guid messageId, Guid crossPostId) => new(messageId, crossPostId);

	[Fact]
	public async Task ThrowCrossPostNotFound_WhenCrossPostNotOwned()
	{
		storage.Setup(x => x.GetRetryInfoAsync(messageId, crossPostId, userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync((CrossPostRetryInfo?)null);

		await Should.ThrowAsync<CrossPostNotFoundException>(async () =>
			await sut.Handle(Command(messageId, crossPostId), CancellationToken.None));

		storage.Verify(
			x => x.RetryAsync(It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
			Times.Never);
	}

	[Theory]
	[InlineData(CrossPostStatus.Published)]
	[InlineData(CrossPostStatus.Pending)]
	[InlineData(CrossPostStatus.InProgress)]
	public async Task ThrowRetryNotAllowed_WhenStatusDisallowsRetry(CrossPostStatus status)
	{
		storage.Setup(x => x.GetRetryInfoAsync(messageId, crossPostId, userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new CrossPostRetryInfo { Status = status, AccountActive = true });

		await Should.ThrowAsync<CrossPostRetryNotAllowedException>(async () =>
			await sut.Handle(Command(messageId, crossPostId), CancellationToken.None));

		storage.Verify(
			x => x.RetryAsync(It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
			Times.Never);
	}

	[Fact]
	public async Task ThrowAccountInactive_WhenAccountNotActive()
	{
		storage.Setup(x => x.GetRetryInfoAsync(messageId, crossPostId, userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new CrossPostRetryInfo { Status = CrossPostStatus.Failed, AccountActive = false });

		await Should.ThrowAsync<SocialAccountInactiveException>(async () =>
			await sut.Handle(Command(messageId, crossPostId), CancellationToken.None));

		storage.Verify(
			x => x.RetryAsync(It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
			Times.Never);
	}

	[Theory]
	[InlineData(CrossPostStatus.Failed)]
	[InlineData(CrossPostStatus.Skipped)]
	public async Task RetryCrossPost_WhenStatusAllowsRetryAndAccountActive(CrossPostStatus status)
	{
		storage.Setup(x => x.GetRetryInfoAsync(messageId, crossPostId, userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new CrossPostRetryInfo { Status = status, AccountActive = true });

		await sut.Handle(Command(messageId, crossPostId), CancellationToken.None);

		storage.Verify(
			x => x.RetryAsync(crossPostId, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
			Times.Once);
	}
}
