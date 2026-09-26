using Moq;
using Security.IdentityServices;
using Shared.Enums;
using Shouldly;
using TgPoster.API.Domain.UseCases.CrossPostTargets.ListCrossPostTargets;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.Tests.CrossPostTargets;

public class ListCrossPostTargetsUseCaseShould
{
	private readonly Guid scheduleId = Guid.NewGuid();
	private readonly Mock<IListCrossPostTargetsStorage> storage = new();
	private readonly ListCrossPostTargetsUseCase sut;
	private readonly Guid userId = Guid.NewGuid();

	public ListCrossPostTargetsUseCaseShould()
	{
		var identity = new Mock<IIdentityProvider>();
		identity.Setup(x => x.Current).Returns(new Identity(userId));

		storage.Setup(x => x.ScheduleExistsAsync(scheduleId, userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);

		sut = new ListCrossPostTargetsUseCase(storage.Object, identity.Object);
	}

	[Fact]
	public async Task ThrowScheduleNotFound_WhenScheduleNotOwned()
	{
		storage.Setup(x => x.ScheduleExistsAsync(scheduleId, userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(false);

		await Should.ThrowAsync<ScheduleNotFoundException>(async () =>
			await sut.Handle(new ListCrossPostTargetsQuery(scheduleId), CancellationToken.None));
	}

	[Fact]
	public async Task ReturnTargetsFromStorage()
	{
		var expected = new List<CrossPostTargetResponse>
		{
			new()
			{
				Id = Guid.NewGuid(),
				ScheduleId = scheduleId,
				SocialAccountId = Guid.NewGuid(),
				Platform = SocialPlatform.Bluesky,
				AccountName = "test.bsky.social",
				AccountStatus = SocialAccountStatus.Active,
				IsActive = true,
				Format = CrossPostFormat.Teaser,
				LinkTarget = CrossPostLinkTarget.Post,
				IncludeMedia = true,
				IncludeParsed = false,
				DelayMinutes = 0
			}
		};
		storage.Setup(x => x.GetAsync(scheduleId, It.IsAny<CancellationToken>())).ReturnsAsync(expected);

		var result = await sut.Handle(new ListCrossPostTargetsQuery(scheduleId), CancellationToken.None);

		result.ShouldBe(expected);
		storage.Verify(x => x.GetAsync(scheduleId, It.IsAny<CancellationToken>()), Times.Once);
	}
}
