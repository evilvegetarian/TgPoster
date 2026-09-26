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
	private readonly Guid ownSessionId = Guid.NewGuid();
	private readonly Guid otherOwnSessionId = Guid.NewGuid();
	private readonly Mock<IUpdateClassifierSettingsStorage> storage;
	private readonly UpdateClassifierSettingsUseCase sut;

	public UpdateClassifierSettingsUseCaseShould()
	{
		storage = new Mock<IUpdateClassifierSettingsStorage>();
		storage.Setup(s => s.GetUserSessionIdsAsync(userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync([ownSessionId, otherOwnSessionId]);
		var identity = new Mock<IIdentityProvider>();
		identity.Setup(x => x.Current).Returns(new Identity(userId));
		sut = new UpdateClassifierSettingsUseCase(storage.Object, identity.Object);
	}

	[Fact]
	public async Task SaveTrimmedModelAndNormalizedCategories_ForCurrentUser()
	{
		var saved = CaptureSaved();

		await sut.Handle(
			ValidCommand() with { Model = "  some/model ", Categories = [" Крипто ", "", "крипто", "Новости"] },
			CancellationToken.None);

		saved.Command.ShouldNotBeNull();
		saved.Command.Model.ShouldBe("some/model");
		saved.Command.Categories.ShouldBe(["Крипто", "Новости"]);
		saved.Command.BatchSize.ShouldBe(3);
		saved.UserId.ShouldBe(userId);
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
		var foreignSessionId = Guid.NewGuid();

		var exception = await Should.ThrowAsync<TelegramSessionEntityNotFoundException>(
			() => sut.Handle(
				ValidCommand() with { TelegramSessionIds = [ownSessionId, foreignSessionId] },
				CancellationToken.None));

		exception.Message.ShouldContain(foreignSessionId.ToString());
		VerifyNotSaved();
	}

	[Fact]
	public async Task SaveSeveralOwnSessions_WithoutDuplicates()
	{
		var saved = CaptureSaved();

		await sut.Handle(
			ValidCommand() with { TelegramSessionIds = [ownSessionId, otherOwnSessionId, ownSessionId] },
			CancellationToken.None);

		saved.Command.ShouldNotBeNull();
		saved.Command.TelegramSessionIds.ShouldBe([ownSessionId, otherOwnSessionId]);
	}

	[Fact]
	public async Task SaveEmptySelection_WithoutCheckingOwnership()
	{
		var saved = CaptureSaved();

		await sut.Handle(ValidCommand(), CancellationToken.None);

		storage.Verify(s => s.GetUserSessionIdsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
		saved.Command.ShouldNotBeNull();
		saved.Command.TelegramSessionIds.ShouldBeEmpty();
	}

	private SavedCall CaptureSaved()
	{
		var call = new SavedCall();
		storage.Setup(s => s.SaveClassifierSettingsAsync(
				It.IsAny<UpdateClassifierSettingsCommand>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
			.Callback<UpdateClassifierSettingsCommand, Guid, CancellationToken>((command, user, _) =>
			{
				call.Command = command;
				call.UserId = user;
			});
		return call;
	}

	private void VerifyNotSaved() =>
		storage.Verify(s => s.SaveClassifierSettingsAsync(
				It.IsAny<UpdateClassifierSettingsCommand>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
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
		[]);

	private sealed class SavedCall
	{
		public UpdateClassifierSettingsCommand? Command { get; set; }
		public Guid UserId { get; set; }
	}
}
