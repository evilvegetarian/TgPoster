using Moq;
using Security.IdentityServices;
using Shouldly;
using TgPoster.API.Domain.UseCases.OpenRouterSetting.DeleteOpenRouterSetting;
using TgPoster.Exceptions.BadRequest;

namespace TgPoster.API.Domain.Tests.OpenRouterSetting;

public sealed class DeleteOpenRouterSettingUseCaseShould
{
	private readonly Mock<IDeleteOpenRouterSettingStorage> storage = new();
	private readonly DeleteOpenRouterSettingUseCase sut;
	private readonly Guid userId = Guid.NewGuid();

	public DeleteOpenRouterSettingUseCaseShould()
	{
		var identity = new Mock<IIdentityProvider>();
		identity.Setup(x => x.Current).Returns(new Identity(userId));
		sut = new DeleteOpenRouterSettingUseCase(storage.Object, identity.Object);
	}

	[Fact]
	public async Task Delete_WhenSettingBelongsToUser()
	{
		var id = Guid.NewGuid();
		storage.Setup(x => x.ExistsAsync(id, userId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

		await sut.Handle(new DeleteOpenRouterSettingCommand(id), CancellationToken.None);

		storage.Verify(x => x.DeleteAsync(id, It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task ThrowNotFound_WhenSettingBelongsToAnotherUser()
	{
		var id = Guid.NewGuid();

		await Should.ThrowAsync<OpenRouterNotFoundException>(() =>
			sut.Handle(new DeleteOpenRouterSettingCommand(id), CancellationToken.None));

		storage.Verify(x => x.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
	}
}
