using Moq;
using Security.IdentityServices;
using Shouldly;
using TgPoster.API.Domain.UseCases.Discover.UpdateClassifierSettings;
using TgPoster.Exceptions.BadRequest;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.Tests.Discover;

public class UpdateClassifierSettingsUseCaseShould
{
	private readonly Guid userId = Guid.NewGuid();
	private readonly Mock<IUpdateClassifierSettingsStorage> storage;
	private readonly UpdateClassifierSettingsUseCase sut;

	public UpdateClassifierSettingsUseCaseShould()
	{
		storage = new Mock<IUpdateClassifierSettingsStorage>();
		var identity = new Mock<IIdentityProvider>();
		identity.Setup(x => x.Current).Returns(new Identity(userId));
		sut = new UpdateClassifierSettingsUseCase(storage.Object, identity.Object);
	}

	[Fact]
	public async Task SaveTrimmedModelAndNormalizedCategories()
	{
		UpdateClassifierSettingsCommand? saved = null;
		storage.Setup(s => s.SaveClassifierSettingsAsync(
				It.IsAny<UpdateClassifierSettingsCommand>(), It.IsAny<CancellationToken>()))
			.Callback<UpdateClassifierSettingsCommand, CancellationToken>((command, _) => saved = command);

		await sut.Handle(
			ValidCommand() with { Model = "  some/model ", Categories = [" Крипто ", "", "крипто", "Новости"] },
			CancellationToken.None);

		saved.ShouldNotBeNull();
		saved.Model.ShouldBe("some/model");
		saved.Categories.ShouldBe(["Крипто", "Новости"]);
		saved.BatchSize.ShouldBe(3);
	}

	[Fact]
	public async Task Throw_WhenModelIsBlank()
	{
		await Should.ThrowAsync<InvalidClassifierSettingsException>(
			() => sut.Handle(ValidCommand() with { Model = "   " }, CancellationToken.None));

		VerifyNotSaved();
	}

	[Fact]
	public async Task Throw_WhenNoCategoriesLeftAfterTrim()
	{
		await Should.ThrowAsync<InvalidClassifierSettingsException>(
			() => sut.Handle(ValidCommand() with { Categories = [" ", ""] }, CancellationToken.None));

		VerifyNotSaved();
	}

	[Fact]
	public async Task Throw_WhenCategoryIsTooLong()
	{
		var tooLong = new string('а', UpdateClassifierSettingsUseCase.MaxCategoryLength + 1);

		await Should.ThrowAsync<InvalidClassifierSettingsException>(
			() => sut.Handle(ValidCommand() with { Categories = ["Ок", tooLong] }, CancellationToken.None));

		VerifyNotSaved();
	}

	[Fact]
	public async Task Throw_WhenPromptHasNoCategoriesPlaceholder()
	{
		await Should.ThrowAsync<InvalidClassifierSettingsException>(
			() => sut.Handle(ValidCommand() with { SystemPrompt = "Просто промпт" }, CancellationToken.None));

		VerifyNotSaved();
	}

	[Fact]
	public async Task Throw_WhenSessionBelongsToAnotherUser()
	{
		var sessionId = Guid.NewGuid();
		storage.Setup(s => s.GetTelegramSessionIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync((Guid?)null);
		storage.Setup(s => s.TelegramSessionBelongsToUserAsync(userId, sessionId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(false);

		await Should.ThrowAsync<TelegramSessionEntityNotFoundException>(
			() => sut.Handle(ValidCommand() with { TelegramSessionId = sessionId }, CancellationToken.None));

		VerifyNotSaved();
	}

	[Fact]
	public async Task Save_WhenSessionBelongsToUser()
	{
		var sessionId = Guid.NewGuid();
		storage.Setup(s => s.GetTelegramSessionIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync((Guid?)null);
		storage.Setup(s => s.TelegramSessionBelongsToUserAsync(userId, sessionId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);

		await sut.Handle(ValidCommand() with { TelegramSessionId = sessionId }, CancellationToken.None);

		storage.Verify(s => s.SaveClassifierSettingsAsync(
				It.Is<UpdateClassifierSettingsCommand>(c => c.TelegramSessionId == sessionId),
				It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task KeepAlreadySavedForeignSession()
	{
		var sessionId = Guid.NewGuid();
		storage.Setup(s => s.GetTelegramSessionIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(sessionId);

		await sut.Handle(ValidCommand() with { TelegramSessionId = sessionId }, CancellationToken.None);

		storage.Verify(s => s.TelegramSessionBelongsToUserAsync(
				It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
			Times.Never);
		storage.Verify(s => s.SaveClassifierSettingsAsync(
				It.IsAny<UpdateClassifierSettingsCommand>(), It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task NotCheckSession_WhenNoneSelected()
	{
		await sut.Handle(ValidCommand(), CancellationToken.None);

		storage.Verify(s => s.GetTelegramSessionIdAsync(It.IsAny<CancellationToken>()), Times.Never);
		storage.Verify(s => s.SaveClassifierSettingsAsync(
				It.Is<UpdateClassifierSettingsCommand>(c => c.TelegramSessionId == null),
				It.IsAny<CancellationToken>()),
			Times.Once);
	}

	private void VerifyNotSaved() =>
		storage.Verify(s => s.SaveClassifierSettingsAsync(
				It.IsAny<UpdateClassifierSettingsCommand>(), It.IsAny<CancellationToken>()),
			Times.Never);

	private static UpdateClassifierSettingsCommand ValidCommand() => new(
		true,
		"qwen/qwen3-vl-8b-instruct",
		3,
		20,
		25,
		6,
		null,
		["Технологии", "Другое"],
		"Выбери из {categories}",
		null);
}
