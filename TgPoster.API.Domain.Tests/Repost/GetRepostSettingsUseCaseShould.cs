using Moq;
using Moq.Language.Flow;
using Security.IdentityServices;
using Shouldly;
using TgPoster.API.Domain.Exceptions;
using TgPoster.API.Domain.UseCases.Repost.GetRepostSettings;

namespace TgPoster.API.Domain.Tests.Repost;

public class GetRepostSettingsUseCaseShould
{
	private readonly Mock<IGetRepostSettingsStorage> storage;
	private readonly Mock<IIdentityProvider> identityProvider;
	private readonly ISetup<IGetRepostSettingsStorage, Task<RepostSettingsResponse?>> getAsyncSetup;
	private readonly GetRepostSettingsUseCase sut;
	private readonly Guid userId = Guid.NewGuid();

	public GetRepostSettingsUseCaseShould()
	{
		storage = new Mock<IGetRepostSettingsStorage>();
		identityProvider = new Mock<IIdentityProvider>();

		var identity = new Identity(userId);
		identityProvider.Setup(x => x.Current).Returns(identity);

		getAsyncSetup = storage.Setup(s =>
			s.GetAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()));

		sut = new GetRepostSettingsUseCase(storage.Object, identityProvider.Object);
	}

	[Fact]
	public async Task ReturnRepostSettings_WhenExists()
	{
		var settingsId = Guid.NewGuid();
		var expectedResponse = new RepostSettingsResponse
		{
			Id = settingsId,
			ScheduleId = Guid.NewGuid(),
			ScheduleName = "Test Schedule",
			TelegramSessionId = Guid.NewGuid(),
			TelegramSessionName = "Test Session",
			IsActive = true,
			Created = DateTimeOffset.UtcNow,
			Destinations = []
		};
		getAsyncSetup.ReturnsAsync(expectedResponse);

		var query = new GetRepostSettingsQuery(settingsId);
		var result = await sut.Handle(query, CancellationToken.None);

		result.ShouldNotBeNull();
		result.Id.ShouldBe(settingsId);
		result.ScheduleName.ShouldBe("Test Schedule");
		storage.Verify(s => s.GetAsync(settingsId, userId, CancellationToken.None), Times.Once);
	}

	[Fact]
	public async Task ThrowRepostSettingsNotFoundException_WhenNotExists()
	{
		var settingsId = Guid.NewGuid();
		getAsyncSetup.ReturnsAsync((RepostSettingsResponse?)null);

		var query = new GetRepostSettingsQuery(settingsId);

		await Should.ThrowAsync<RepostSettingsNotFoundException>(
			async () => await sut.Handle(query, CancellationToken.None));

		storage.Verify(s => s.GetAsync(settingsId, userId, CancellationToken.None), Times.Once);
	}

	[Fact]
	public async Task UseCurrentUserIdFromIdentityProvider()
	{
		var settingsId = Guid.NewGuid();
		var expectedResponse = new RepostSettingsResponse
		{
			Id = settingsId,
			ScheduleId = Guid.NewGuid(),
			ScheduleName = "Test",
			TelegramSessionId = Guid.NewGuid(),
			TelegramSessionName = null,
			IsActive = true,
			Created = DateTimeOffset.UtcNow,
			Destinations = []
		};
		getAsyncSetup.ReturnsAsync(expectedResponse);

		await sut.Handle(new GetRepostSettingsQuery(settingsId), CancellationToken.None);

		storage.Verify(s => s.GetAsync(settingsId, userId, It.IsAny<CancellationToken>()), Times.Once);
		identityProvider.Verify(x => x.Current, Times.Once);
	}

	[Fact]
	public async Task ReturnResponseWithDestinations_WhenDestinationsExist()
	{
		var settingsId = Guid.NewGuid();
		var destinations = new List<RepostDestinationDto>
		{
			new() { Id = Guid.NewGuid(), ChatId = -1001234567890, IsActive = true },
			new() { Id = Guid.NewGuid(), ChatId = -1009876543210, IsActive = false }
		};
		var expectedResponse = new RepostSettingsResponse
		{
			Id = settingsId,
			ScheduleId = Guid.NewGuid(),
			ScheduleName = "Test",
			TelegramSessionId = Guid.NewGuid(),
			TelegramSessionName = "Session",
			IsActive = true,
			Created = DateTimeOffset.UtcNow,
			Destinations = destinations
		};
		getAsyncSetup.ReturnsAsync(expectedResponse);

		var result = await sut.Handle(new GetRepostSettingsQuery(settingsId), CancellationToken.None);

		result.Destinations.Count.ShouldBe(2);
		result.Destinations.ShouldContain(d => d.ChatId == -1001234567890 && d.IsActive);
		result.Destinations.ShouldContain(d => d.ChatId == -1009876543210 && !d.IsActive);
	}
}
