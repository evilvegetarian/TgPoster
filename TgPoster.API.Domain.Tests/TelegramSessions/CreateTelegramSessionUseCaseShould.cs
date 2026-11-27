using Moq;
using Security.IdentityServices;
using Shouldly;
using TgPoster.API.Domain.UseCases.Proxies.ListProxies;
using TgPoster.API.Domain.UseCases.TelegramBots.ListTelegramBot;
using TgPoster.API.Domain.UseCases.TelegramSessions;
using TgPoster.API.Domain.UseCases.TelegramSessions.CreateTelegramSession;
using TgPoster.Exceptions.BadRequest;
using TgPoster.Exceptions.NotFound;
using TgPoster.Telegram.Abstractions;

namespace TgPoster.API.Domain.Tests.TelegramSessions;

public sealed class CreateTelegramSessionUseCaseShould
{
	private readonly Guid userId = Guid.NewGuid();
	private readonly Mock<ICreateTelegramSessionStorage> storage = new();
	private readonly Mock<IListProxiesStorage> proxyStorage = new();
	private readonly Mock<IListTelegramBotStorage> botStorage = new();
	private readonly Mock<ITelegramAuthService> authService = new();
	private readonly CreateTelegramSessionUseCase sut;

	public CreateTelegramSessionUseCaseShould()
	{
		var identity = new Mock<IIdentityProvider>();
		identity.SetupGet(x => x.Current).Returns(new Identity(userId));
		sut = new(storage.Object, proxyStorage.Object, botStorage.Object, identity.Object, authService.Object);
	}

	[Fact]
	public async Task ReuseApiCredentials_WhenCredentialsSessionBelongsToCurrentUser()
	{
		var sourceSessionId = Guid.NewGuid();
		var createdId = Guid.NewGuid();
		storage.Setup(x => x.GetApiCredentialsAsync(userId, sourceSessionId, CancellationToken.None))
			.ReturnsAsync(new TelegramApiCredentials("123456", "reused-api-hash"));
		storage.Setup(x => x.CreateAsync(userId, "123456", "reused-api-hash", "+79990000001", "second", null, null,
				CancellationToken.None))
			.ReturnsAsync(new CreateTelegramSessionResponse(createdId, "second", true, string.Empty));
		authService.Setup(x => x.StartAuthAsync(createdId, CancellationToken.None)).ReturnsAsync("code_sent");

		var response = await sut.Handle(
			new(null, null, "+79990000001", "second", null, null, sourceSessionId),
			CancellationToken.None);

		response.Id.ShouldBe(createdId);
		response.AuthStatus.ShouldBe("code_sent");
		storage.VerifyAll();
		authService.Verify(x => x.StartAuthAsync(createdId, CancellationToken.None), Times.Once);
	}

	[Fact]
	public async Task ThrowNotFound_WhenCredentialsSessionIsMissingOrForeign()
	{
		var missingId = Guid.NewGuid();
		storage.Setup(x => x.GetApiCredentialsAsync(userId, missingId, CancellationToken.None))
			.ReturnsAsync((TelegramApiCredentials?)null);

		await Should.ThrowAsync<TelegramSessionEntityNotFoundException>(() => sut.Handle(
			new(null, null, "+79990000001", "second", null, null, missingId),
			CancellationToken.None));

		storage.Verify(
			x => x.CreateAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
				It.IsAny<string?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
			Times.Never);
		authService.Verify(x => x.StartAuthAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[Theory]
	[InlineData("", "hash")]
	[InlineData("123", "")]
	public async Task ThrowCorrupted_WhenSourceSessionHasEmptyApiCredentials(string apiId, string apiHash)
	{
		var sourceSessionId = Guid.NewGuid();
		storage.Setup(x => x.GetApiCredentialsAsync(userId, sourceSessionId, CancellationToken.None))
			.ReturnsAsync(new TelegramApiCredentials(apiId, apiHash));

		await Should.ThrowAsync<TelegramSessionCorruptedException>(() => sut.Handle(
			new(null, null, "+79990000001", "second", null, null, sourceSessionId),
			CancellationToken.None));

		storage.Verify(
			x => x.CreateAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
				It.IsAny<string?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
			Times.Never);
	}

	[Theory]
	[InlineData(null, "hash")]
	[InlineData("123", null)]
	[InlineData("   ", "hash")]
	[InlineData("123", "   ")]
	public async Task ThrowInvalidCredentials_WhenNeitherCredentialsNorSourceSessionProvided(
		string? apiId,
		string? apiHash
	)
	{
		await Should.ThrowAsync<InvalidTelegramApiCredentialsException>(() => sut.Handle(
			new(apiId, apiHash, "+79990000001", "second", null, null),
			CancellationToken.None));

		storage.VerifyNoOtherCalls();
		authService.VerifyNoOtherCalls();
	}

	[Fact]
	public async Task CreateSessionWithManualApiCredentials_WhenCredentialsSessionIsNull()
	{
		var createdId = Guid.NewGuid();
		storage.Setup(x => x.CreateAsync(userId, "654321", "manual-hash", "+79990000002", "manual", null, null,
				CancellationToken.None))
			.ReturnsAsync(new CreateTelegramSessionResponse(createdId, "manual", true, string.Empty));
		authService.Setup(x => x.StartAuthAsync(createdId, CancellationToken.None)).ReturnsAsync("code_sent");

		var response = await sut.Handle(
			new(" 654321 ", " manual-hash ", "+79990000002", "manual", null, null),
			CancellationToken.None);

		response.Id.ShouldBe(createdId);
		storage.VerifyAll();
	}
}
