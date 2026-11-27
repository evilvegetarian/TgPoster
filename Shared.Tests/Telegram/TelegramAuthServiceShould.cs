using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shared.Enums;
using Shouldly;
using TgPoster.Exceptions.BadRequest;
using TgPoster.Telegram.Abstractions;
using TgPoster.Telegram.Internal;
using TgPoster.Telegram.Models;
using TL;
using WTelegram;

namespace Shared.Tests.Telegram;

public sealed class TelegramAuthServiceShould : IAsyncLifetime
{
	private readonly Mock<ITelegramAuthRepository> repository = new();
	private readonly ServiceProvider services;
	private readonly TelegramClientManager manager = new(NullLogger<TelegramClientManager>.Instance);
	private readonly SessionDataDebouncer debouncer;
	private readonly TelegramAuthService sut;
	private readonly Guid id = Guid.NewGuid();
	private Func<Client, string, Task<string?>> login = (_, _) => Task.FromResult<string?>(null);

	public TelegramAuthServiceShould()
	{
		services = new ServiceCollection().AddSingleton(repository.Object).BuildServiceProvider();
		debouncer = new(services.GetRequiredService<IServiceScopeFactory>(), NullLogger<SessionDataDebouncer>.Instance);
		sut = new(NullLogger<TelegramAuthService>.Instance, repository.Object, debouncer, manager,
			Mock.Of<ITelegramSessionAlertService>(), (client, value) => login(client, value));
	}

	public Task InitializeAsync() => Task.CompletedTask;
	public async Task DisposeAsync() { manager.Dispose(); await debouncer.DisposeAsync(); await services.DisposeAsync(); }

	[Fact]
	public async Task StartLogin_AndRememberAuthorizedState()
	{
		repository.Setup(x => x.GetByIdAsync(id, CancellationToken.None)).ReturnsAsync(Session(true));

		var result = await sut.StartAuthAsync(id, CancellationToken.None);

		result.ShouldBe("already_authorized");
		repository.Verify(x => x.UpdateStatusAsync(id, TelegramSessionStatus.Authorized, CancellationToken.None), Times.Once);
	}

	[Theory]
	[InlineData("verification_code", "code_sent", TelegramSessionStatus.CodeSent)]
	[InlineData("password", "awaiting_password", TelegramSessionStatus.AwaitingPassword)]
	public async Task RecordPendingLoginStep(string state, string expected, TelegramSessionStatus status)
	{
		repository.Setup(x => x.GetByIdAsync(id, CancellationToken.None)).ReturnsAsync(Session(true));
		login = (_, _) => Task.FromResult<string?>(state);

		var result = await sut.StartAuthAsync(id, CancellationToken.None);

		result.ShouldBe(expected);
		repository.Verify(x => x.UpdateStatusAsync(id, status, CancellationToken.None), Times.Once);
		manager.TryGetPendingClient(id, out _).ShouldBeTrue();
	}

	[Fact]
	public async Task ReplacePendingClient_WhenStartingLoginAgain()
	{
		repository.Setup(x => x.GetByIdAsync(id, CancellationToken.None)).ReturnsAsync(Session(true));
		login = (_, _) => Task.FromResult<string?>("verification_code");

		await sut.StartAuthAsync(id, CancellationToken.None);
		manager.TryGetPendingClient(id, out var firstClient).ShouldBeTrue();

		await sut.StartAuthAsync(id, CancellationToken.None);
		manager.TryGetPendingClient(id, out var secondClient).ShouldBeTrue();

		secondClient.ShouldNotBeSameAs(firstClient);
	}

	[Fact]
	public async Task ClearPendingClient_AndRecordFailure_WhenStartingLoginFails()
	{
		repository.Setup(x => x.GetByIdAsync(id, CancellationToken.None)).ReturnsAsync(Session(true));
		login = (_, _) => Task.FromException<string?>(new IOException("offline"));

		await Should.ThrowAsync<IOException>(() => sut.StartAuthAsync(id, CancellationToken.None));

		manager.TryGetPendingClient(id, out _).ShouldBeFalse();
		repository.Verify(x => x.UpdateStatusAsync(id, TelegramSessionStatus.Failed, CancellationToken.None), Times.Once);
	}

	[Theory]
	[InlineData(null, true, false)]
	[InlineData("password", false, true)]
	[InlineData("unexpected", false, false)]
	public async Task VerifyCode_AndReportNextAuthenticationStep(string? state, bool success, bool password)
	{
		using var client = PendingClient();
		login = (_, value) =>
		{
			value.ShouldBe("12345");
			return Task.FromResult(state);
		};

		var result = await sut.VerifyCodeAsync(id, "12345", CancellationToken.None);

		result.Success.ShouldBe(success);
		result.RequiresPassword.ShouldBe(password);
		manager.TryGetPendingClient(id, out _).ShouldBe(!success);
		if (success) repository.Verify(x => x.UpdateStatusAsync(id, TelegramSessionStatus.Authorized, CancellationToken.None), Times.Once);
		if (password) repository.Verify(x => x.UpdateStatusAsync(id, TelegramSessionStatus.AwaitingPassword, CancellationToken.None), Times.Once);
	}

	[Fact]
	public async Task KeepPendingClient_WhenCodeIsInvalid()
	{
		using var client = PendingClient();
		login = (_, _) => Task.FromException<string?>(new RpcException(400, "PHONE_CODE_INVALID"));

		var result = await sut.VerifyCodeAsync(id, "wrong", CancellationToken.None);

		result.Success.ShouldBeFalse();
		result.Message.ShouldBe("invalid_code");
		manager.TryGetPendingClient(id, out _).ShouldBeTrue();
	}

	[Theory]
	[InlineData(null, true)]
	[InlineData("password", false)]
	public async Task VerifySecondFactor(string? state, bool expected)
	{
		using var client = PendingClient();
		login = (_, value) =>
		{
			value.ShouldBe("test-password");
			return Task.FromResult(state);
		};

		var result = await sut.SendPasswordAsync(id, "test-password", CancellationToken.None);

		result.ShouldBe(expected);
		if (expected) repository.Verify(x => x.UpdateStatusAsync(id, TelegramSessionStatus.Authorized, CancellationToken.None), Times.Once);
	}

	[Fact]
	public async Task RejectInvalidSecondFactor_WithoutMarkingSessionFailed()
	{
		using var client = PendingClient();
		login = (_, _) => Task.FromException<string?>(new RpcException(400, "PASSWORD_HASH_INVALID"));

		(await sut.SendPasswordAsync(id, "wrong", CancellationToken.None)).ShouldBeFalse();

		repository.Verify(x => x.UpdateStatusAsync(id, TelegramSessionStatus.Failed, It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task ResolveSessionsByRequestedPurpose()
	{
		repository.Setup(x => x.GetByTelegramSessionPurpose(TelegramSessionPurpose.Discover, CancellationToken.None)).ReturnsAsync(id);
		repository.Setup(x => x.GetSessionIdsByTelegramSessionPurpose(TelegramSessionPurpose.Discover, CancellationToken.None))
			.ReturnsAsync([id]);

		(await sut.GetSessionIdForPurposeAsync(TelegramSessionPurpose.Discover)).ShouldBe(id);
		(await sut.GetSessionIdsForPurposeAsync(TelegramSessionPurpose.Discover)).ShouldBe([id]);
	}

	[Fact]
	public async Task ReturnNoClient_WhenPurposeHasNoSession()
	{
		(await sut.GetClientAsync(TelegramSessionPurpose.Discover)).ShouldBeNull();
		repository.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task RejectMissingSession_BeforeStartingLogin()
	{
		await Should.ThrowAsync<TelegramSessionNotFoundException>(() => sut.StartAuthAsync(id, CancellationToken.None));
		await Should.ThrowAsync<TelegramSessionNotFoundException>(() => sut.GetClientAsync(id));
	}

	[Theory]
	[InlineData("", "hash")]
	[InlineData("123", "")]
	public async Task RejectSessionWithMissingApiCredentials(string apiId, string apiHash)
	{
		repository.Setup(x => x.GetByIdAsync(id, CancellationToken.None)).ReturnsAsync(new TelegramSessionDto
		{
			Id = id, UserId = Guid.NewGuid(), ApiId = apiId, ApiHash = apiHash, PhoneNumber = "+10000000000", IsActive = true
		});

		await Should.ThrowAsync<TelegramSessionCorruptedException>(() => sut.StartAuthAsync(id, CancellationToken.None));
	}

	[Fact]
	public async Task RejectInactiveSession_BeforeConnecting()
	{
		repository.Setup(x => x.GetByIdAsync(id, CancellationToken.None)).ReturnsAsync(Session(false));

		await Should.ThrowAsync<TelegramSessionInactiveException>(() => sut.GetClientAsync(id));
	}

	[Fact]
	public async Task RejectSessionWithoutAuthorizationData()
	{
		repository.Setup(x => x.GetByIdAsync(id, CancellationToken.None)).ReturnsAsync(Session(true));

		await Should.ThrowAsync<TelegramSessionNotAuthorizedException>(() => sut.GetClientAsync(id));
	}

	[Fact]
	public async Task RejectCode_WhenLoginWasNotStarted()
	{
		await Should.ThrowAsync<TelegramAuthSessionNotFoundException>(() => sut.VerifyCodeAsync(id, "12345", CancellationToken.None));
	}

	private TelegramSessionDto Session(bool active) => new()
	{
		Id = id, UserId = Guid.NewGuid(), ApiId = "123", ApiHash = "00112233445566778899aabbccddeeff",
		PhoneNumber = "+10000000000", IsActive = active
	};

	private Client PendingClient()
	{
		var client = new Client(key => key switch
		{
			"api_id" => "123", "api_hash" => "00112233445566778899aabbccddeeff", "phone_number" => "+10000000000", _ => null
		}, null, _ => { });
		manager.AddPendingClient(id, client);
		return client;
	}
}
