using Moq;
using Security.IdentityServices;
using Shared.Discover;
using Shouldly;
using TgPoster.API.Domain.UseCases.Discover.GetDiscoverSettings;

namespace TgPoster.API.Domain.Tests.Discover;

public class GetDiscoverSettingsUseCaseShould
{
	private readonly Guid userId = Guid.NewGuid();
	private readonly Mock<IGetDiscoverSettingsStorage> storage;
	private readonly GetDiscoverSettingsUseCase sut;

	public GetDiscoverSettingsUseCaseShould()
	{
		storage = new Mock<IGetDiscoverSettingsStorage>();
		storage.Setup(s => s.GetDiscoverSessionsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);
		var identity = new Mock<IIdentityProvider>();
		identity.Setup(x => x.Current).Returns(new Identity(userId));
		sut = new GetDiscoverSettingsUseCase(storage.Object, identity.Object);
	}

	[Fact]
	public async Task ReturnDefaults_WhenNothingSaved()
	{
		storage.Setup(s => s.GetDiscoverSettingsAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync((SavedDiscoverSettingsDto?)null);

		var result = await sut.Handle(new GetDiscoverSettingsQuery(), CancellationToken.None);

		result.IsEnabled.ShouldBe(DiscoverDefaults.IsEnabled);
		result.IntervalMinutes.ShouldBe(DiscoverDefaults.IntervalMinutes);
		result.ChannelsPerSession.ShouldBe(DiscoverDefaults.ChannelsPerSession);
		result.Sessions.ShouldBeEmpty();
		result.UpdatedAt.ShouldBeNull();
	}

	[Fact]
	public async Task ReturnSavedValues()
	{
		var updatedAt = DateTimeOffset.UtcNow.AddHours(-1);
		storage.Setup(s => s.GetDiscoverSettingsAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(new SavedDiscoverSettingsDto
			{
				IsEnabled = false,
				IntervalMinutes = 15,
				ChannelsPerSession = 4,
				UpdatedAt = updatedAt
			});

		var result = await sut.Handle(new GetDiscoverSettingsQuery(), CancellationToken.None);

		result.IsEnabled.ShouldBeFalse();
		result.IntervalMinutes.ShouldBe(15);
		result.ChannelsPerSession.ShouldBe(4);
		result.UpdatedAt.ShouldBe(updatedAt);
	}

	[Fact]
	public async Task ReturnSessionsOfCurrentUser()
	{
		var sessions = new List<DiscoverSessionOption>
		{
			new() { Id = Guid.NewGuid(), IsActive = true, IsAuthorized = true, IsSelected = true, IsOwn = true },
			new() { Id = Guid.NewGuid(), IsActive = true, IsAuthorized = true, IsSelected = true, IsOwn = false }
		};
		storage.Setup(s => s.GetDiscoverSettingsAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync((SavedDiscoverSettingsDto?)null);
		storage.Setup(s => s.GetDiscoverSessionsAsync(userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(sessions);

		var result = await sut.Handle(new GetDiscoverSettingsQuery(), CancellationToken.None);

		result.Sessions.ShouldBe(sessions);
	}
}
