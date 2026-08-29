using Security.Cryptography;
using Shouldly;
using TgPoster.Storage.ConfigModels;
using TgPoster.Storage.Data;
using TgPoster.Storage.Repositories;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public sealed class TelegramSessionAlertRepositoryShould : IClassFixture<StorageTestFixture>
{
	private const string SecretKey = "12345678901234567890123456789012";

	private readonly PosterContext context;
	private readonly ICryptoAES crypto = new CryptoAES();
	private readonly TelegramSessionAlertRepository sut;

	public TelegramSessionAlertRepositoryShould(StorageTestFixture fixture)
	{
		context = fixture.GetDbContext();
		sut = new TelegramSessionAlertRepository(
			fixture.GetDbContext(),
			crypto,
			new TelegramSecretOptions { SecretKey = SecretKey });
	}

	[Fact]
	public async Task GetAlertTargetAsync_WithNotificationBot_ShouldReturnDecryptedToken()
	{
		const string token = "123456:super-secret-bot-token";
		var user = await new UserBuilder(context).CreateAsync();
		var bot = await new TelegramBotBuilder(context)
			.WithOwnerId(user.Id)
			.WithApiTelegram(crypto.Encrypt(SecretKey, token))
			.WithChatId(555)
			.CreateAsync();
		var session = await new TelegramSessionBuilder(context)
			.WithUserId(user.Id)
			.WithName("Основной")
			.WithPhoneNumber("+79991234567")
			.WithNotificationBotId(bot.Id)
			.CreateAsync();

		var result = await sut.GetAlertTargetAsync(session.Id, CancellationToken.None);

		result.ShouldNotBeNull();
		result.BotToken.ShouldBe(token);
		result.ChatId.ShouldBe(555);
		result.SessionName.ShouldBe("Основной");
		result.PhoneNumber.ShouldBe("+79991234567");
	}

	[Fact]
	public async Task GetAlertTargetAsync_WithoutNotificationBot_ShouldReturnNull()
	{
		var user = await new UserBuilder(context).CreateAsync();
		var session = await new TelegramSessionBuilder(context)
			.WithUserId(user.Id)
			.CreateAsync();

		var result = await sut.GetAlertTargetAsync(session.Id, CancellationToken.None);

		result.ShouldBeNull();
	}

	[Fact]
	public async Task GetAlertTargetAsync_WithUnknownSession_ShouldReturnNull()
	{
		var result = await sut.GetAlertTargetAsync(Guid.NewGuid(), CancellationToken.None);

		result.ShouldBeNull();
	}
}
