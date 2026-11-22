using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shared.Enums;
using Shouldly;
using TgPoster.Telegram.Abstractions;
using TgPoster.Telegram.Internal;
using TgPoster.Telegram.Models;
using TL;
using WTelegram;

namespace Shared.Tests.Telegram;

public sealed class TelegramMessageExecutionShould
{
	private readonly Guid sessionId = Guid.NewGuid();
	private readonly Mock<ITelegramSessionAlertService> alerts = new();
	private readonly TelegramMessageService sut;

	public TelegramMessageExecutionShould() => sut = new(NullLogger<TelegramMessageService>.Instance,
		new MissingClientResolver(), alerts.Object);

	[Fact]
	public async Task ReturnSuccessfulValue()
	{
		var result = await sut.ExecuteAsync(sessionId, () => Task.FromResult(42), "test", false, CancellationToken.None);

		result.IsSuccess.ShouldBeTrue();
		result.Value.ShouldBe(42);
		alerts.VerifyNoOtherCalls();
	}

	[Theory]
	[InlineData("USERNAME_NOT_OCCUPIED", TelegramOperationStatus.UsernameNotFound)]
	[InlineData("USERNAME_INVALID", TelegramOperationStatus.UsernameNotFound)]
	[InlineData("AUTH_KEY_UNREGISTERED", TelegramOperationStatus.SessionNotFound)]
	[InlineData("SESSION_REVOKED", TelegramOperationStatus.SessionNotFound)]
	[InlineData("SESSION_EXPIRED", TelegramOperationStatus.SessionNotFound)]
	[InlineData("USER_DEACTIVATED", TelegramOperationStatus.SessionNotFound)]
	[InlineData("USER_DEACTIVATED_BAN", TelegramOperationStatus.SessionNotFound)]
	[InlineData("CHANNEL_PRIVATE", TelegramOperationStatus.ChannelBanned)]
	[InlineData("USER_BANNED_IN_CHANNEL", TelegramOperationStatus.ChannelBanned)]
	[InlineData("CHAT_RESTRICTED", TelegramOperationStatus.ChannelBanned)]
	[InlineData("CHAT_WRITE_FORBIDDEN", TelegramOperationStatus.NoWritePermission)]
	[InlineData("CHAT_SEND_PLAIN_FORBIDDEN", TelegramOperationStatus.NoWritePermission)]
	[InlineData("ALLOW_PAYMENT_REQUIRED_10", TelegramOperationStatus.NoWritePermission)]
	[InlineData("CHAT_SEND_PHOTOS_FORBIDDEN", TelegramOperationStatus.NoMediaPermission)]
	[InlineData("CHAT_SEND_VIDEOS_FORBIDDEN", TelegramOperationStatus.NoMediaPermission)]
	[InlineData("TOPIC_CLOSED", TelegramOperationStatus.TopicClosed)]
	[InlineData("TOPIC_DELETED", TelegramOperationStatus.TopicClosed)]
	[InlineData("CHAT_FORWARDS_RESTRICTED", TelegramOperationStatus.ForwardsRestricted)]
	[InlineData("PEER_FLOOD", TelegramOperationStatus.SpamRestricted)]
	[InlineData("CHANNEL_INVALID", TelegramOperationStatus.AccessDenied)]
	[InlineData("PEER_ID_INVALID", TelegramOperationStatus.AccessDenied)]
	[InlineData("CHAT_ADMIN_REQUIRED", TelegramOperationStatus.AccessDenied)]
	[InlineData("MESSAGE_ID_INVALID", TelegramOperationStatus.AccessDenied)]
	[InlineData("UNEXPECTED_RPC_ERROR", TelegramOperationStatus.UnknownError)]
	public async Task ClassifyRpcFailures(string message, TelegramOperationStatus expected)
	{
		var result = await sut.ExecuteAsync(sessionId, () => Task.FromException<int>(new RpcException(400, message)),
			"test", false, CancellationToken.None);

		result.Status.ShouldBe(expected);
		result.IsSuccess.ShouldBeFalse();
		result.ErrorMessage.ShouldNotBeNullOrEmpty();
		if (expected == TelegramOperationStatus.SessionNotFound)
			alerts.Verify(x => x.NotifyAsync(sessionId, TelegramSessionProblem.AuthorizationRevoked,
				It.IsAny<string>(), CancellationToken.None), Times.Once);
		if (expected == TelegramOperationStatus.SpamRestricted)
			alerts.Verify(x => x.NotifyAsync(sessionId, TelegramSessionProblem.SpamRestricted,
				It.IsAny<string>(), CancellationToken.None), Times.Once);
	}

	[Theory]
	[InlineData("FLOOD_WAIT", TelegramOperationStatus.FloodWait)]
	[InlineData("SLOWMODE_WAIT", TelegramOperationStatus.SlowMode)]
	public async Task ReturnServerCooldownWithoutWaiting(string message, TelegramOperationStatus expected)
	{
		var result = await sut.ExecuteAsync(sessionId, () => Task.FromException<int>(new RpcException(420, message, 120)),
			"test", false, CancellationToken.None);

		result.Status.ShouldBe(expected);
		result.FloodWaitSeconds.ShouldBe(120);
	}

	[Fact]
	public async Task DistinguishInternalTimeoutFromCallerCancellation()
	{
		var timeout = await sut.ExecuteAsync(sessionId, () => Task.FromException<int>(new OperationCanceledException()),
			"test", false, CancellationToken.None);
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();

		await Should.ThrowAsync<OperationCanceledException>(() => sut.ExecuteAsync(sessionId,
			() => Task.FromException<int>(new OperationCanceledException(cancellation.Token)), "test", false, cancellation.Token));

		timeout.Status.ShouldBe(TelegramOperationStatus.Timeout);
	}

	[Fact]
	public async Task ReportUnknownErrors_WithoutThrowing()
	{
		var result = await sut.ExecuteAsync(sessionId, () => Task.FromException<int>(new IOException("transport failed")),
			"test", false, CancellationToken.None);

		result.Status.ShouldBe(TelegramOperationStatus.UnknownError);
		result.ErrorMessage.ShouldBe("transport failed");
	}

	[Fact]
	public async Task ReturnSessionNotFound_WhenResolverHasNoClient()
	{
		var result = await sut.ResolveChannelAsync(sessionId, "channel");

		result.Status.ShouldBe(TelegramOperationStatus.SessionNotFound);
		alerts.VerifyNoOtherCalls();
	}

	[Fact]
	public async Task ReportMissingSession_WhenLoadingChannelRecommendations()
	{
		var result = await sut.GetChannelRecommendationsAsync(sessionId, TelegramPeer.Channel(10, 123), default);

		result.Status.ShouldBe(TelegramOperationStatus.SessionNotFound);
		result.Value.ShouldBeNull();
		alerts.VerifyNoOtherCalls();
	}

	private sealed class MissingClientResolver : ITelegramClientResolver
	{
		public Task<Client?> GetClientAsync(Guid sessionId, CancellationToken ct = default) => Task.FromResult<Client?>(null);
		public Task<Client?> GetClientAsync(TelegramSessionPurpose purpose, CancellationToken ct = default) => Task.FromResult<Client?>(null);
	}
}
