using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shared.Enums;
using Shared.Social.Bluesky;
using Shouldly;
using TgPoster.Worker.Domain.UseCases.CrossPosting.Media;
using TgPoster.Worker.Domain.UseCases.CrossPosting.Publishing;
using TgPoster.Worker.Domain.UseCases.CrossPosting.Publishing.Bluesky;

namespace TgPoster.Worker.Domain.Tests.CrossPosting;

public class BlueskyPublisherShould
{
	private static readonly DateTimeOffset FixedNow = new(2026, 9, 26, 12, 0, 0, TimeSpan.Zero);
	private static readonly BlueskySession Session = new("did:plc:test", "test.handle", "access-jwt", "refresh-jwt", "https://pds.test");
	private static readonly LoadedImage Image1 = new([0x01], 100, 80);
	private static readonly LoadedImage Image2 = new([0x02], 120, 90);

	private readonly Mock<IBlueskyClient> client = new();
	private readonly MemoryCache cache = new(new MemoryCacheOptions());
	private readonly BlueskyPublisher sut;

	public BlueskyPublisherShould()
	{
		var sessionCache = new BlueskySessionCache(cache);
		sut = new BlueskyPublisher(client.Object, sessionCache, new FixedTimeProvider(FixedNow), NullLogger<BlueskyPublisher>.Instance);
		client.Setup(c => c.CreateSessionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(BlueskyResult<BlueskySession>.Ok(Session));
	}

	[Fact]
	public async Task Publish_SinglePartWithImages_ShouldUploadImagesAndAttachToFirstPart()
	{
		BlueskyPostRecord? capturedRecord = null;
		client.Setup(c => c.UploadBlobAsync(Session, Image1.Data, "image/jpeg", It.IsAny<CancellationToken>()))
			.ReturnsAsync(BlueskyResult<BlueskyBlob>.Ok(new BlueskyBlob("link1", "image/jpeg", 100)));
		client.Setup(c => c.UploadBlobAsync(Session, Image2.Data, "image/jpeg", It.IsAny<CancellationToken>()))
			.ReturnsAsync(BlueskyResult<BlueskyBlob>.Ok(new BlueskyBlob("link2", "image/jpeg", 100)));
		client.Setup(c => c.CreatePostAsync(Session, It.IsAny<BlueskyPostRecord>(), It.IsAny<CancellationToken>()))
			.Callback<BlueskySession, BlueskyPostRecord, CancellationToken>((_, r, _) => capturedRecord = r)
			.ReturnsAsync(BlueskyResult<BlueskyRecordRef>.Ok(new BlueskyRecordRef("at://did/root", "cid-root")));

		var request = CreateRequest(parts: ["Читайте в Telegram t.me/chan/42"], images: [Image1, Image2], linkText: "t.me/chan/42", linkUrl: "https://t.me/chan/42");

		var result = await sut.PublishAsync(request, CancellationToken.None);

		result.IsSuccess.ShouldBeTrue();
		client.Verify(c => c.UploadBlobAsync(Session, Image1.Data, "image/jpeg", It.IsAny<CancellationToken>()), Times.Once);
		client.Verify(c => c.UploadBlobAsync(Session, Image2.Data, "image/jpeg", It.IsAny<CancellationToken>()), Times.Once);
		capturedRecord.ShouldNotBeNull();
		capturedRecord.Images.Count.ShouldBe(2);
		capturedRecord.External.ShouldBeNull();
		capturedRecord.Facets.Count.ShouldBe(1);
		capturedRecord.Facets[0].Uri.ShouldBe("https://t.me/chan/42");
	}

	[Fact]
	public async Task Publish_NoImagesWithLink_ShouldSetExternalOnlyOnLastPart()
	{
		var records = new List<BlueskyPostRecord>();
		client.Setup(c => c.CreatePostAsync(Session, It.IsAny<BlueskyPostRecord>(), It.IsAny<CancellationToken>()))
			.Callback<BlueskySession, BlueskyPostRecord, CancellationToken>((_, r, _) => records.Add(r))
			.ReturnsAsync((BlueskySession _, BlueskyPostRecord _, CancellationToken _) =>
				BlueskyResult<BlueskyRecordRef>.Ok(new BlueskyRecordRef($"at://did/post{records.Count}", $"cid-{records.Count}")));

		var request = CreateRequest(
			parts: ["Первая часть", "Вторая часть", "Третья часть"],
			linkUrl: "https://t.me/chan/42");

		await sut.PublishAsync(request, CancellationToken.None);

		records.Count.ShouldBe(3);
		records[0].External.ShouldBeNull();
		records[1].External.ShouldBeNull();
		records[2].External.ShouldNotBeNull();
		records[2].External!.Uri.ShouldBe("https://t.me/chan/42");
	}

	[Fact]
	public async Task Publish_ChainOfThreeParts_ShouldSetReplyRefsCorrectly()
	{
		var records = new List<BlueskyPostRecord>();
		client.Setup(c => c.CreatePostAsync(Session, It.IsAny<BlueskyPostRecord>(), It.IsAny<CancellationToken>()))
			.Callback<BlueskySession, BlueskyPostRecord, CancellationToken>((_, r, _) => records.Add(r))
			.ReturnsAsync((BlueskySession _, BlueskyPostRecord _, CancellationToken _) =>
				BlueskyResult<BlueskyRecordRef>.Ok(new BlueskyRecordRef($"at://did/post{records.Count}", $"cid-{records.Count}")));

		var request = CreateRequest(parts: ["Часть 1", "Часть 2", "Часть 3"]);

		await sut.PublishAsync(request, CancellationToken.None);

		records.Count.ShouldBe(3);
		records[0].Reply.ShouldBeNull();
		records[1].Reply.ShouldNotBeNull();
		records[1].Reply!.Root.Uri.ShouldBe("at://did/post1");
		records[1].Reply!.Parent.Uri.ShouldBe("at://did/post1");
		records[2].Reply.ShouldNotBeNull();
		records[2].Reply!.Root.Uri.ShouldBe("at://did/post1");
		records[2].Reply!.Parent.Uri.ShouldBe("at://did/post2");
	}

	[Fact]
	public async Task Publish_Success_ShouldReturnBskyAppUrl()
	{
		client.Setup(c => c.CreatePostAsync(Session, It.IsAny<BlueskyPostRecord>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(BlueskyResult<BlueskyRecordRef>.Ok(new BlueskyRecordRef("at://did:plc:test/app.bsky.feed.post/abc123", "cid")));

		var request = CreateRequest(parts: ["Hello"]);

		var result = await sut.PublishAsync(request, CancellationToken.None);

		result.IsSuccess.ShouldBeTrue();
		result.ExternalPostId.ShouldBe("at://did:plc:test/app.bsky.feed.post/abc123");
		result.ExternalUrl.ShouldBe("https://bsky.app/profile/test.handle/post/abc123");
	}

	[Fact]
	public async Task Publish_LoginAuthError_ShouldReturnAuthAndNotCreatePost()
	{
		client.Setup(c => c.CreateSessionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(BlueskyResult<BlueskySession>.Fail(BlueskyErrorKind.Auth, "InvalidCredentials"));

		var request = CreateRequest(parts: ["Hello"]);

		var result = await sut.PublishAsync(request, CancellationToken.None);

		result.IsSuccess.ShouldBeFalse();
		result.ErrorKind.ShouldBe(SocialPublishErrorKind.Auth);
		client.Verify(c => c.CreatePostAsync(It.IsAny<BlueskySession>(), It.IsAny<BlueskyPostRecord>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task CreatePost_AuthOnce_ShouldReLoginAndRetrySamePart()
	{
		var records = new List<BlueskyPostRecord>();
		var callCount = 0;
		client.Setup(c => c.CreatePostAsync(Session, It.IsAny<BlueskyPostRecord>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((BlueskySession _, BlueskyPostRecord r, CancellationToken _) =>
			{
				records.Add(r);
				callCount++;
				return callCount == 1
					? BlueskyResult<BlueskyRecordRef>.Fail(BlueskyErrorKind.Auth, "ExpiredToken")
					: BlueskyResult<BlueskyRecordRef>.Ok(new BlueskyRecordRef("at://did/root", "cid-root"));
			});

		var request = CreateRequest(parts: ["Hello"]);

		var result = await sut.PublishAsync(request, CancellationToken.None);

		result.IsSuccess.ShouldBeTrue();
		client.Verify(c => c.CreateSessionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
		records.Count.ShouldBe(2);
		records[0].Text.ShouldBe(records[1].Text);
	}

	[Fact]
	public async Task CreatePost_AfterReLogin_ShouldUseNewSessionForNextParts()
	{
		var expired = Session with { AccessJwt = "expired-jwt" };
		var fresh = Session with { AccessJwt = "fresh-jwt" };
		client.SetupSequence(c => c.CreateSessionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(BlueskyResult<BlueskySession>.Ok(expired))
			.ReturnsAsync(BlueskyResult<BlueskySession>.Ok(fresh));
		client.Setup(c => c.CreatePostAsync(expired, It.IsAny<BlueskyPostRecord>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(BlueskyResult<BlueskyRecordRef>.Fail(BlueskyErrorKind.Auth, "ExpiredToken"));
		var postNumber = 0;
		client.Setup(c => c.CreatePostAsync(fresh, It.IsAny<BlueskyPostRecord>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(() =>
			{
				postNumber++;
				return BlueskyResult<BlueskyRecordRef>.Ok(new BlueskyRecordRef($"at://did/post{postNumber}", $"cid{postNumber}"));
			});
		var request = CreateRequest(parts: ["Первая часть", "Вторая часть"]);

		var result = await sut.PublishAsync(request, CancellationToken.None);

		result.IsSuccess.ShouldBeTrue();
		client.Verify(c => c.CreateSessionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
		client.Verify(c => c.CreatePostAsync(expired, It.IsAny<BlueskyPostRecord>(), It.IsAny<CancellationToken>()), Times.Once);
		client.Verify(c => c.CreatePostAsync(fresh, It.IsAny<BlueskyPostRecord>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
	}

	[Fact]
	public async Task Publish_ErrorOnSecondPart_ShouldReturnPermanentWithRootUrl()
	{
		var callCount = 0;
		client.Setup(c => c.CreatePostAsync(Session, It.IsAny<BlueskyPostRecord>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((BlueskySession _, BlueskyPostRecord _, CancellationToken _) =>
			{
				callCount++;
				return callCount == 1
					? BlueskyResult<BlueskyRecordRef>.Ok(new BlueskyRecordRef("at://did/post1", "cid1"))
					: BlueskyResult<BlueskyRecordRef>.Fail(BlueskyErrorKind.Permanent, "Rate limited");
			});

		var request = CreateRequest(parts: ["Часть 1", "Часть 2"]);

		var result = await sut.PublishAsync(request, CancellationToken.None);

		result.IsSuccess.ShouldBeFalse();
		result.ErrorKind.ShouldBe(SocialPublishErrorKind.Permanent);
		result.Error!.ShouldContain("Цепочка опубликована частично");
		result.ExternalUrl.ShouldBe("https://bsky.app/profile/test.handle/post/post1");
	}

	[Fact]
	public async Task Publish_TwoTimesForSameAccount_ShouldCreateSessionOnce()
	{
		client.Setup(c => c.CreatePostAsync(Session, It.IsAny<BlueskyPostRecord>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(BlueskyResult<BlueskyRecordRef>.Ok(new BlueskyRecordRef("at://did/post", "cid")));

		var accountId = Guid.NewGuid();
		var request1 = CreateRequest(accountId: accountId, parts: ["Первый"]);
		var request2 = CreateRequest(accountId: accountId, parts: ["Второй"]);

		await sut.PublishAsync(request1, CancellationToken.None);
		await sut.PublishAsync(request2, CancellationToken.None);

		client.Verify(c => c.CreateSessionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task Publish_ClientThrowsHttpRequestException_ShouldReturnTransient()
	{
		client.Setup(c => c.CreateSessionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.ThrowsAsync(new HttpRequestException("No connection"));

		var request = CreateRequest(parts: ["Hello"]);

		var result = await sut.PublishAsync(request, CancellationToken.None);

		result.IsSuccess.ShouldBeFalse();
		result.ErrorKind.ShouldBe(SocialPublishErrorKind.Transient);
	}

	private static SocialPublishRequest CreateRequest(
		Guid? accountId = null,
		IReadOnlyList<string>? parts = null,
		IReadOnlyList<LoadedImage>? images = null,
		string? linkText = null,
		string? linkUrl = null)
	{
		return new SocialPublishRequest
		{
			CrossPostId = Guid.NewGuid(),
			AccountId = accountId ?? Guid.NewGuid(),
			AccountName = "test.handle",
			AccountExternalUserId = "did:plc:test",
			AccountSecret = "app-password",
			Parts = parts ?? ["Hello"],
			Images = images ?? [],
			LinkText = linkText,
			LinkUrl = linkUrl
		};
	}

	private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
	{
		public override DateTimeOffset GetUtcNow() => now;
	}
}
