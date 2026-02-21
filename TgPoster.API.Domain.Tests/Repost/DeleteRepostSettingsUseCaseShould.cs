using MediatR;
using Moq;
using Moq.Language.Flow;
using Shouldly;
using TgPoster.API.Domain.Exceptions;
using TgPoster.API.Domain.UseCases.Repost.DeleteRepostSettings;

namespace TgPoster.API.Domain.Tests.Repost;

public class DeleteRepostSettingsUseCaseShould
{
	private readonly Mock<IDeleteRepostSettingsStorage> storage;
	private readonly ISetup<IDeleteRepostSettingsStorage, Task<bool>> existsSetup;
	private readonly DeleteRepostSettingsUseCase sut;

	public DeleteRepostSettingsUseCaseShould()
	{
		storage = new Mock<IDeleteRepostSettingsStorage>();
		existsSetup = storage.Setup(s =>
			s.RepostSettingsExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()));

		sut = new DeleteRepostSettingsUseCase(storage.Object);
	}

	[Fact]
	public async Task DeleteSettings_WhenExists()
	{
		var id = Guid.NewGuid();
		existsSetup.ReturnsAsync(true);

		await sut.Handle(new DeleteRepostSettingsCommand(id), CancellationToken.None);

		storage.Verify(s => s.DeleteRepostSettingsAsync(id, It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task ThrowRepostSettingsNotFoundException_WhenNotExists()
	{
		var id = Guid.NewGuid();
		existsSetup.ReturnsAsync(false);

		await Should.ThrowAsync<RepostSettingsNotFoundException>(
			async () => await sut.Handle(new DeleteRepostSettingsCommand(id), CancellationToken.None));

		storage.Verify(s => s.DeleteRepostSettingsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task ReturnUnit_WhenSuccessful()
	{
		existsSetup.ReturnsAsync(true);

		var result = await sut.Handle(new DeleteRepostSettingsCommand(Guid.NewGuid()), CancellationToken.None);

		result.ShouldBe(Unit.Value);
	}
}
