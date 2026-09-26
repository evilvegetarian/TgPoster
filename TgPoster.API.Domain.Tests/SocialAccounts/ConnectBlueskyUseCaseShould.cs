using Moq;
using Security.Cryptography;
using Security.IdentityServices;
using Shared.Enums;
using Shared.Social.Bluesky;
using Shouldly;
using TgPoster.API.Domain.ConfigModels;
using TgPoster.API.Domain.UseCases.SocialAccounts.ConnectBluesky;
using TgPoster.Exceptions.BadRequest;

namespace TgPoster.API.Domain.Tests.SocialAccounts;

public class ConnectBlueskyUseCaseShould
{
	private readonly Mock<IConnectBlueskyStorage> storage;
	private readonly Mock<IBlueskyClient> blueskyClient;
	private readonly Mock<ICryptoAES> crypto;
	private readonly ConnectBlueskyUseCase sut;
	private readonly Guid userId = Guid.NewGuid();

	public ConnectBlueskyUseCaseShould()
	{
		storage = new Mock<IConnectBlueskyStorage>();
		blueskyClient = new Mock<IBlueskyClient>();
		crypto = new Mock<ICryptoAES>();
		var identityProvider = new Mock<IIdentityProvider>();
		var telegramOptions = new TelegramOptions { SecretKey = "secret" };

		identityProvider.Setup(x => x.Current).Returns(new Identity(userId));
		crypto.Setup(x => x.Encrypt(It.IsAny<string>(), It.IsAny<string>())).Returns("enc");

		sut = new ConnectBlueskyUseCase(
			storage.Object,
			identityProvider.Object,
			blueskyClient.Object,
			crypto.Object,
			telegramOptions);
	}

	[Fact]
	public async Task ReturnId_WhenSessionSucceeded()
	{
		var expectedId = Guid.NewGuid();
		var handle = "test.bsky.social";
		var did = "did:plc:abc";
		blueskyClient.Setup(x => x.CreateSessionAsync(handle, It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(BlueskyResult<BlueskySession>.Ok(new BlueskySession(did, handle, "jwt", "refresh", "pds")));
		storage.Setup(x => x.UpsertAsync(userId, SocialPlatform.Bluesky, handle, did, "enc", null, It.IsAny<CancellationToken>()))
			.ReturnsAsync(expectedId);

		var result = await sut.Handle(new ConnectBlueskyCommand("  @test.bsky.social  ", "app-pass"), CancellationToken.None);

		result.Id.ShouldBe(expectedId);
		blueskyClient.Verify(x => x.CreateSessionAsync(handle, "app-pass", It.IsAny<CancellationToken>()), Times.Once);
		storage.Verify(
			x => x.UpsertAsync(userId, SocialPlatform.Bluesky, handle, did, "enc", null, It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task ThrowInvalidSocialCredentialsException_WhenSessionFailed()
	{
		blueskyClient.Setup(x => x.CreateSessionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(BlueskyResult<BlueskySession>.Fail(BlueskyErrorKind.Auth, "bad credentials"));

		await Should.ThrowAsync<InvalidSocialCredentialsException>(async () =>
			await sut.Handle(new ConnectBlueskyCommand("test.bsky.social", "wrong"), CancellationToken.None));

		storage.Verify(
			x => x.UpsertAsync(It.IsAny<Guid>(), It.IsAny<SocialPlatform>(), It.IsAny<string>(), It.IsAny<string>(),
				It.IsAny<string>(), It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()),
			Times.Never);
	}
}
