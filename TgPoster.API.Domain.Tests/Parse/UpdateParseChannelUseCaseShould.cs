using Moq;
using Security.IdentityServices;
using TgPoster.API.Domain.UseCases.Parse.UpdateParseChannel;

namespace TgPoster.API.Domain.Tests.Parse;

public sealed class UpdateParseChannelUseCaseShould
{
	private readonly Mock<IUpdateParseChannelStorage> storage = new();
	private readonly UpdateParseChannelUseCase sut;
	private readonly Guid userId = Guid.NewGuid();

	public UpdateParseChannelUseCaseShould()
	{
		var identity = new Mock<IIdentityProvider>();
		identity.Setup(x => x.Current).Returns(new Identity(userId));

		sut = new UpdateParseChannelUseCase(identity.Object, storage.Object);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task PassSkipAdsToStorage_WhenUpdating(bool skipAds)
	{
		var command = new UpdateParseChannelCommand(Guid.NewGuid(), "source_channel", true, Guid.NewGuid(), false,
			false, [], false, null, null, false, Guid.NewGuid(), skipAds);
		storage.Setup(x => x.ExistParseChannelAsync(command.Id, userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);

		await sut.Handle(command, CancellationToken.None);

		storage.Verify(x => x.UpdateParseChannelAsync(
			It.Is<UpdateParseChannelCommand>(c => c.Id == command.Id && c.SkipAds == skipAds),
			It.IsAny<CancellationToken>()), Times.Once);
	}
}
