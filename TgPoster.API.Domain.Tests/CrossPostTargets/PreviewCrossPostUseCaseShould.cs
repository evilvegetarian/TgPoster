using Moq;
using Security.IdentityServices;
using Shared.Enums;
using Shouldly;
using TgPoster.API.Domain.UseCases.CrossPostTargets.PreviewCrossPost;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.Tests.CrossPostTargets;

public class PreviewCrossPostUseCaseShould
{
	private readonly Guid accountId = Guid.NewGuid();
	private readonly Guid scheduleId = Guid.NewGuid();
	private readonly Mock<IPreviewCrossPostStorage> storage = new();
	private readonly PreviewCrossPostUseCase sut;
	private readonly Guid userId = Guid.NewGuid();

	public PreviewCrossPostUseCaseShould()
	{
		var identity = new Mock<IIdentityProvider>();
		identity.Setup(x => x.Current).Returns(new Identity(userId));
		sut = new PreviewCrossPostUseCase(storage.Object, identity.Object);
	}

	private static CrossPostPreviewTargetDto Target(
		Guid accountId,
		SocialPlatform platform,
		string accountName = "acc",
		CrossPostFormat format = CrossPostFormat.Teaser,
		CrossPostLinkTarget linkTarget = CrossPostLinkTarget.Post,
		string? customLink = null,
		string? callToAction = null) => new()
	{
		SocialAccountId = accountId,
		Platform = platform,
		AccountName = accountName,
		Format = format,
		LinkTarget = linkTarget,
		CustomLink = customLink,
		CallToAction = callToAction
	};

	private static PreviewCrossPostQuery Query(
		Guid scheduleId,
		Guid? accountId = null,
		CrossPostFormat? format = null,
		CrossPostLinkTarget? linkTarget = null,
		string? customLink = null,
		string? callToAction = null,
		string? text = null) =>
		new(scheduleId, accountId, format, linkTarget, customLink, callToAction, text);

	private void SetupContext(string channelName = "channel", string? lastMessageText = "Привет",
		params CrossPostPreviewTargetDto[] targets)
	{
		storage.Setup(x => x.GetContextAsync(scheduleId, userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new CrossPostPreviewContext
			{
				ChannelName = channelName,
				LastMessageText = lastMessageText,
				Targets = [.. targets]
			});
	}

	[Fact]
	public async Task ThrowScheduleNotFound_WhenContextMissing()
	{
		storage.Setup(x => x.GetContextAsync(scheduleId, userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync((CrossPostPreviewContext?)null);

		await Should.ThrowAsync<ScheduleNotFoundException>(async () =>
			await sut.Handle(Query(scheduleId), CancellationToken.None));
	}

	[Fact]
	public async Task UseLastMessageText_WhenTextNotProvided()
	{
		SetupContext(lastMessageText: "Текст последнего поста", targets: Target(accountId, SocialPlatform.Bluesky));

		var result = await sut.Handle(Query(scheduleId), CancellationToken.None);

		result.Single().Parts.ShouldContain(x => x.Text.Contains("Текст последнего поста"));
	}

	[Fact]
	public async Task OverrideFormat_FromRequest()
	{
		SetupContext(lastMessageText: "Привет", targets: Target(accountId, SocialPlatform.Bluesky,
			format: CrossPostFormat.Announcement));

		var result = await sut.Handle(Query(scheduleId, format: CrossPostFormat.Teaser), CancellationToken.None);

		result.Single().Format.ShouldBe(CrossPostFormat.Teaser);
		result.Single().Parts[0].Text.ShouldContain("Привет");
	}

	[Fact]
	public async Task PreviewByAccount_WhenTargetMissing()
	{
		SetupContext(lastMessageText: "Привет");
		storage.Setup(x => x.GetAccountAsync(accountId, userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Target(accountId, SocialPlatform.Bluesky, "account-only"));

		var result = await sut.Handle(Query(scheduleId, accountId: accountId), CancellationToken.None);

		result.Single().AccountName.ShouldBe("account-only");
	}

	[Fact]
	public async Task ThrowSocialAccountNotFound_WhenAccountNotOwned()
	{
		SetupContext(lastMessageText: "Привет");
		storage.Setup(x => x.GetAccountAsync(accountId, userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync((CrossPostPreviewTargetDto?)null);

		await Should.ThrowAsync<SocialAccountNotFoundException>(async () =>
			await sut.Handle(Query(scheduleId, accountId: accountId), CancellationToken.None));
	}

	[Fact]
	public async Task BuildShortLink_ForBlueskyPost()
	{
		SetupContext(lastMessageText: "Привет", targets: Target(accountId, SocialPlatform.Bluesky,
			linkTarget: CrossPostLinkTarget.Post));

		var result = await sut.Handle(Query(scheduleId), CancellationToken.None);

		result.Single().Parts.ShouldContain(x => x.Text.Contains("t.me/channel/123"));
	}
}
