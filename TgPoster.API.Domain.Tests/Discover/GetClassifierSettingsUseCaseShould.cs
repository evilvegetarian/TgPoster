using Moq;
using Security.IdentityServices;
using Shared.Classification;
using Shouldly;
using TgPoster.API.Domain.UseCases.Discover.GetClassifierSettings;

namespace TgPoster.API.Domain.Tests.Discover;

public class GetClassifierSettingsUseCaseShould
{
	private readonly Guid userId = Guid.NewGuid();
	private readonly Mock<IGetClassifierSettingsStorage> storage;
	private readonly GetClassifierSettingsUseCase sut;

	public GetClassifierSettingsUseCaseShould()
	{
		storage = new Mock<IGetClassifierSettingsStorage>();
		storage.Setup(s => s.GetClassifierSessionsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);
		var identity = new Mock<IIdentityProvider>();
		identity.Setup(x => x.Current).Returns(new Identity(userId));
		sut = new GetClassifierSettingsUseCase(storage.Object, identity.Object);
	}

	[Fact]
	public async Task ReturnDefaults_WhenNothingSaved()
	{
		storage.Setup(s => s.GetClassifierSettingsAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync((SavedClassifierSettingsDto?)null);

		var result = await sut.Handle(new GetClassifierSettingsQuery(), CancellationToken.None);

		result.IsEnabled.ShouldBe(ClassifierDefaults.IsEnabled);
		result.Model.ShouldBe(ClassifierDefaults.Model);
		result.BatchSize.ShouldBe(ClassifierDefaults.BatchSize);
		result.IntervalMinutes.ShouldBe(ClassifierDefaults.IntervalMinutes);
		result.MessageSampleCount.ShouldBe(ClassifierDefaults.MessageSampleCount);
		result.PhotoCount.ShouldBe(ClassifierDefaults.PhotoCount);
		result.ReclassifyAfterDays.ShouldBeNull();
		result.Categories.ShouldBe(ClassifierDefaults.Categories);
		result.SystemPrompt.ShouldBe(ClassifierDefaults.SystemPrompt);
		result.Sessions.ShouldBeEmpty();
		result.UpdatedAt.ShouldBeNull();
	}

	[Fact]
	public async Task ReturnSavedValues_WithDefaultsForReset()
	{
		var updatedAt = DateTimeOffset.UtcNow.AddHours(-1);
		storage.Setup(s => s.GetClassifierSettingsAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(new SavedClassifierSettingsDto
			{
				IsEnabled = false,
				Model = "custom/model",
				BatchSize = 7,
				IntervalMinutes = 5,
				MessageSampleCount = 50,
				PhotoCount = 0,
				ReclassifyAfterDays = 30,
				Categories = ["Один", "Два"],
				SystemPrompt = "Свой промпт {categories}",
				UpdatedAt = updatedAt
			});

		var result = await sut.Handle(new GetClassifierSettingsQuery(), CancellationToken.None);

		result.IsEnabled.ShouldBeFalse();
		result.Model.ShouldBe("custom/model");
		result.BatchSize.ShouldBe(7);
		result.IntervalMinutes.ShouldBe(5);
		result.MessageSampleCount.ShouldBe(50);
		result.PhotoCount.ShouldBe(0);
		result.ReclassifyAfterDays.ShouldBe(30);
		result.Categories.ShouldBe(["Один", "Два"]);
		result.SystemPrompt.ShouldBe("Свой промпт {categories}");
		result.UpdatedAt.ShouldBe(updatedAt);
		result.DefaultSystemPrompt.ShouldBe(ClassifierDefaults.SystemPrompt);
		result.DefaultCategories.ShouldBe(ClassifierDefaults.Categories);
		result.CategoriesPlaceholder.ShouldBe(ClassifierDefaults.CategoriesPlaceholder);
	}

	[Fact]
	public async Task ReturnSessionsOfCurrentUser()
	{
		var sessions = new List<ClassifierSessionOption>
		{
			new() { Id = Guid.NewGuid(), IsActive = true, IsAuthorized = true, IsSelected = true, IsOwn = true },
			new() { Id = Guid.NewGuid(), IsActive = true, IsAuthorized = true, IsSelected = true, IsOwn = false }
		};
		storage.Setup(s => s.GetClassifierSettingsAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync((SavedClassifierSettingsDto?)null);
		storage.Setup(s => s.GetClassifierSessionsAsync(userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(sessions);

		var result = await sut.Handle(new GetClassifierSettingsQuery(), CancellationToken.None);

		result.Sessions.ShouldBe(sessions);
	}
}
