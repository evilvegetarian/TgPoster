using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Security.Cryptography;
using Shared.Enums;
using Shouldly;
using TgPoster.Worker.Domain.ConfigModels;
using TgPoster.Worker.Domain.UseCases.CrossPosting;
using TgPoster.Worker.Domain.UseCases.CrossPosting.Media;
using TgPoster.Worker.Domain.UseCases.CrossPosting.Publishing;

namespace TgPoster.Worker.Domain.Tests.CrossPosting;

public class CrossPostPublisherShould
{
	private static readonly DateTimeOffset FixedNow = new(2026, 9, 26, 12, 0, 0, TimeSpan.Zero);

	private readonly Mock<ICrossPostWorkerStorage> storage;
	private readonly Mock<ICrossPostMediaLoader> mediaLoader;
	private readonly Mock<ICryptoAES> crypto;
	private readonly TelegramOptions telegramOptions;

	public CrossPostPublisherShould()
	{
		storage = new Mock<ICrossPostWorkerStorage>();
		mediaLoader = new Mock<ICrossPostMediaLoader>();
		crypto = new Mock<ICryptoAES>();
		crypto
			.Setup(x => x.Decrypt(It.IsAny<string>(), It.IsAny<string>()))
			.Returns((string _, string value) => value + "-dec");
		telegramOptions = new TelegramOptions { SecretKey = "k", TelegramSessionId = Guid.Empty };
	}

	[Fact]
	public async Task Publish_Success_ShouldMarkPublished()
	{
		var publisher = CreatePublisher(SocialPlatform.Bluesky, SocialPublishResult.Success("post-id", "https://bsky.app/post"));
		var (sut, job) = CreateSutWithJob([publisher.Object], SocialPlatform.Bluesky);

		await sut.PublishAsync(job.Id, CancellationToken.None);

		storage.Verify(s => s.MarkPublishedAsync(job.Id, "post-id", "https://bsky.app/post", FixedNow, It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task Publish_ShouldPassLinkUrlAndLinkText_ForBlueskyPostTarget()
	{
		SocialPublishRequest? capturedRequest = null;
		var publisher = new Mock<ISocialPublisher>();
		publisher.Setup(p => p.Platform).Returns(SocialPlatform.Bluesky);
		publisher
			.Setup(p => p.PublishAsync(It.IsAny<SocialPublishRequest>(), It.IsAny<CancellationToken>()))
			.Callback<SocialPublishRequest, CancellationToken>((r, _) => capturedRequest = r)
			.ReturnsAsync(SocialPublishResult.Success("post-id", null));

		var (sut, job) = CreateSutWithJob(
			[publisher.Object],
			SocialPlatform.Bluesky,
			linkTarget: CrossPostLinkTarget.Post,
			channelName: "@chan",
			telegramMessageId: 42,
			includeMedia: false);

		await sut.PublishAsync(job.Id, CancellationToken.None);

		capturedRequest.ShouldNotBeNull();
		capturedRequest.LinkUrl.ShouldBe("https://t.me/chan/42");
		capturedRequest.LinkText.ShouldBe("t.me/chan/42");
		capturedRequest.Parts[^1].ShouldContain("t.me/chan/42");
	}

	[Fact]
	public async Task Publish_ShouldPassDecryptedSecret()
	{
		SocialPublishRequest? capturedRequest = null;
		var publisher = new Mock<ISocialPublisher>();
		publisher.Setup(p => p.Platform).Returns(SocialPlatform.Bluesky);
		publisher
			.Setup(p => p.PublishAsync(It.IsAny<SocialPublishRequest>(), It.IsAny<CancellationToken>()))
			.Callback<SocialPublishRequest, CancellationToken>((r, _) => capturedRequest = r)
			.ReturnsAsync(SocialPublishResult.Success("post-id", null));

		var (sut, job) = CreateSutWithJob([publisher.Object], SocialPlatform.Bluesky, accountSecretEncrypted: "secret", includeMedia: false);

		await sut.PublishAsync(job.Id, CancellationToken.None);

		capturedRequest.ShouldNotBeNull();
		capturedRequest.AccountSecret.ShouldBe("secret-dec");
	}

	[Fact]
	public async Task Publish_WithoutMedia_ShouldNotCallMediaLoader()
	{
		var publisher = CreatePublisher(SocialPlatform.Bluesky, SocialPublishResult.Success("post-id", null));
		var (sut, job) = CreateSutWithJob([publisher.Object], SocialPlatform.Bluesky, includeMedia: false);

		await sut.PublishAsync(job.Id, CancellationToken.None);

		mediaLoader.Verify(m => m.LoadAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<CrossPostFileDto>>(), It.IsAny<MediaProfile>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task Publish_TransientWithOneAttempt_ShouldRescheduleForFiveMinutes()
	{
		var publisher = CreatePublisher(SocialPlatform.Bluesky, SocialPublishResult.Failure(SocialPublishErrorKind.Transient, "retry"));
		var (sut, job) = CreateSutWithJob([publisher.Object], SocialPlatform.Bluesky, attempts: 1);

		await sut.PublishAsync(job.Id, CancellationToken.None);

		storage.Verify(s => s.RescheduleAsync(job.Id, FixedNow.AddMinutes(5), "retry", It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task Publish_TransientWithThreeAttempts_ShouldFail()
	{
		var publisher = CreatePublisher(SocialPlatform.Bluesky, SocialPublishResult.Failure(SocialPublishErrorKind.Transient, "retry"));
		var (sut, job) = CreateSutWithJob([publisher.Object], SocialPlatform.Bluesky, attempts: 3);

		await sut.PublishAsync(job.Id, CancellationToken.None);

		storage.Verify(s => s.MarkFailedAsync(job.Id, "retry", It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task Publish_AuthError_ShouldMarkAccountNeedsReauthAndFail()
	{
		var accountId = Guid.NewGuid();
		var publisher = CreatePublisher(SocialPlatform.Bluesky, SocialPublishResult.Failure(SocialPublishErrorKind.Auth, "bad auth"));
		var (sut, job) = CreateSutWithJob([publisher.Object], SocialPlatform.Bluesky, socialAccountId: accountId);

		await sut.PublishAsync(job.Id, CancellationToken.None);

		storage.Verify(s => s.MarkAccountNeedsReauthAsync(accountId, "bad auth", It.IsAny<CancellationToken>()), Times.Once);
		storage.Verify(s => s.MarkFailedAsync(job.Id, "bad auth", It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task Publish_WithoutPublisher_ShouldFailWithPlatformNotSupported()
	{
		var (sut, job) = CreateSutWithJob([], SocialPlatform.Bluesky);

		await sut.PublishAsync(job.Id, CancellationToken.None);

		storage.Verify(s => s.MarkFailedAsync(job.Id, "Площадка не поддерживается", It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task Publish_JobNotFound_ShouldSkip()
	{
		var sut = new CrossPostPublisher(
			storage.Object,
			[],
			mediaLoader.Object,
			crypto.Object,
			telegramOptions,
			new FixedTimeProvider(FixedNow),
			NullLogger<CrossPostPublisher>.Instance);
		var id = Guid.NewGuid();
		storage.Setup(s => s.GetJobAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((CrossPostJobDto?)null);

		await sut.PublishAsync(id, CancellationToken.None);

		storage.Verify(s => s.MarkSkippedAsync(id, "Данные для публикации не найдены (пост, настройка или аккаунт удалены)", It.IsAny<CancellationToken>()), Times.Once);
	}

	private (CrossPostPublisher Sut, CrossPostJobDto Job) CreateSutWithJob(
		IReadOnlyList<ISocialPublisher> publishers,
		SocialPlatform platform,
		Guid? id = null,
		Guid? socialAccountId = null,
		CrossPostLinkTarget linkTarget = CrossPostLinkTarget.Post,
		string channelName = "channel",
		int? telegramMessageId = null,
		bool includeMedia = true,
		string? accountSecretEncrypted = null,
		int attempts = 0)
	{
		var job = new CrossPostJobDto
		{
			Id = id ?? Guid.NewGuid(),
			MessageId = Guid.NewGuid(),
			SocialAccountId = socialAccountId ?? Guid.NewGuid(),
			Platform = platform,
			Attempts = attempts,
			TextMessage = "Hello world",
			Format = CrossPostFormat.Teaser,
			LinkTarget = linkTarget,
			CustomLink = null,
			CallToAction = null,
			IncludeMedia = includeMedia,
			ChannelName = channelName,
			TelegramMessageId = telegramMessageId,
			BotTokenEncrypted = "bot-token",
			AccountName = "account",
			AccountExternalUserId = "did:plc:test",
			AccountSecretEncrypted = accountSecretEncrypted ?? "secret",
			Files = []
		};
		storage.Setup(s => s.GetJobAsync(job.Id, It.IsAny<CancellationToken>())).ReturnsAsync(job);

		var sut = new CrossPostPublisher(
			storage.Object,
			publishers,
			mediaLoader.Object,
			crypto.Object,
			telegramOptions,
			new FixedTimeProvider(FixedNow),
			NullLogger<CrossPostPublisher>.Instance);

		return (sut, job);
	}

	private static Mock<ISocialPublisher> CreatePublisher(SocialPlatform platform, SocialPublishResult result)
	{
		var publisher = new Mock<ISocialPublisher>();
		publisher.Setup(p => p.Platform).Returns(platform);
		publisher
			.Setup(p => p.PublishAsync(It.IsAny<SocialPublishRequest>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(result);
		return publisher;
	}

	private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
	{
		public override DateTimeOffset GetUtcNow() => now;
	}
}
