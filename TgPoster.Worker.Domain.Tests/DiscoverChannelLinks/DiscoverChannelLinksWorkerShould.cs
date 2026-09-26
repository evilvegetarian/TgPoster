using Hangfire.MemoryStorage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shared.Enums;
using Shouldly;
using TgPoster.Telegram.Abstractions;
using TgPoster.Telegram.Models;
using TgPoster.Worker.Domain.UseCases.DiscoverChannelLinks;
using TgPoster.Worker.Domain.UseCases.WorkerJobStatus;

namespace TgPoster.Worker.Domain.Tests.DiscoverChannelLinks;

public class DiscoverChannelLinksWorkerShould
{
	private readonly Mock<IDiscoverChannelLinksStorage> storage = new();
	private readonly Mock<ITelegramAuthService> authService = new();
	private readonly Mock<ITelegramMessageService> tgMessages = new();
	private readonly Mock<ITelegramPublicLookupService> publicLookup = new();
	private readonly Mock<IWorkerJobStatusStorage> statusStorage = new();
	private readonly Guid firstSessionId = Guid.NewGuid();
	private readonly Guid secondSessionId = Guid.NewGuid();
	private readonly DiscoverChannelDto firstChannel = new() { Id = Guid.NewGuid(), Username = "first_chat" };
	private readonly DiscoverChannelDto secondChannel = new() { Id = Guid.NewGuid(), Username = "second_chat" };

	public DiscoverChannelLinksWorkerShould()
	{
		SetupSessions(firstSessionId);
		SetupQueue(firstChannel);
	}

	[Theory]
	[InlineData(TelegramOperationStatus.AccessDenied)]
	[InlineData(TelegramOperationStatus.UnknownError)]
	public async Task MarkChannelAsError_WhenChannelCannotBeResolved(TelegramOperationStatus status)
	{
		SetupResolve(firstChannel, TelegramOperationResult<TelegramChatInfo>.Failed(status, "fail"));
		var sut = CreateSut();

		await sut.ProcessChannelsAsync();

		storage.Verify(s => s.MarkAsErrorAsync(firstChannel.Id, It.IsAny<CancellationToken>()), Times.Once);
		statusStorage.Verify(s => s.ReportCompletedAsync(
			WorkerJobNames.DiscoverChannelLinks, It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()), Times.Once);
	}

	[Theory]
	[InlineData(TelegramOperationStatus.Timeout)]
	[InlineData(TelegramOperationStatus.SessionNotFound)]
	[InlineData(TelegramOperationStatus.SpamRestricted)]
	public async Task KeepChannelInQueue_WhenResolveFailsNotBecauseOfChannel(TelegramOperationStatus status)
	{
		SetupResolve(firstChannel, TelegramOperationResult<TelegramChatInfo>.Failed(status, "fail"));
		var sut = CreateSut();

		await sut.ProcessChannelsAsync();

		VerifyNeverMarkedAsError();
		storage.Verify(s => s.ChannelBanned(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task ReportCooldown_AndKeepChannelInQueue_WhenResolveGetsFloodWait()
	{
		SetupResolve(firstChannel,
			TelegramOperationResult<TelegramChatInfo>.Failed(TelegramOperationStatus.FloodWait, "flood", 600));
		var startedAt = DateTimeOffset.UtcNow;
		var sut = CreateSut();

		await sut.ProcessChannelsAsync();

		VerifyNeverMarkedAsError();
		statusStorage.Verify(s => s.ReportCooldownAsync(
			WorkerJobNames.DiscoverChannelLinks,
			It.Is<DateTimeOffset>(until => until >= startedAt.AddSeconds(600)),
			It.IsAny<DateTimeOffset?>(),
			It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task MarkChannelAsBanned_NotAsError_WhenUsernameNotFound()
	{
		SetupResolve(firstChannel,
			TelegramOperationResult<TelegramChatInfo>.Failed(TelegramOperationStatus.UsernameNotFound, "gone"));
		var sut = CreateSut();

		await sut.ProcessChannelsAsync();

		storage.Verify(s => s.ChannelBanned(firstChannel.Id, It.IsAny<CancellationToken>()), Times.Once);
		VerifyNeverMarkedAsError();
	}

	[Fact]
	public async Task MarkChannelAsError_AndReportFailure_WhenEveryChannelThrows()
	{
		SetupResolveThrows(firstChannel, "boom");
		var sut = CreateSut();

		await Should.NotThrowAsync(() => sut.ProcessChannelsAsync());

		storage.Verify(s => s.MarkAsErrorAsync(firstChannel.Id, It.IsAny<CancellationToken>()), Times.Once);
		statusStorage.Verify(s => s.ReportFailedAsync(
			WorkerJobNames.DiscoverChannelLinks, "boom", It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task ReportCompleted_WhenOnlySomeChannelsThrow()
	{
		SetupSessions(firstSessionId, secondSessionId);
		SetupQueue(firstChannel, secondChannel);
		SetupResolveThrows(firstChannel, "boom");
		SetupResolve(secondChannel,
			TelegramOperationResult<TelegramChatInfo>.Failed(TelegramOperationStatus.AccessDenied, "private"));
		var sut = CreateSut();

		await sut.ProcessChannelsAsync();

		storage.Verify(s => s.MarkAsErrorAsync(firstChannel.Id, It.IsAny<CancellationToken>()), Times.Once);
		storage.Verify(s => s.MarkAsErrorAsync(secondChannel.Id, It.IsAny<CancellationToken>()), Times.Once);
		statusStorage.Verify(s => s.ReportCompletedAsync(
			WorkerJobNames.DiscoverChannelLinks, It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()), Times.Once);
		statusStorage.Verify(s => s.ReportFailedAsync(
				It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()),
			Times.Never);
	}

	private void SetupSessions(params Guid[] sessionIds) =>
		authService
			.Setup(s => s.GetSessionIdsForPurposeAsync(TelegramSessionPurpose.Discover, It.IsAny<CancellationToken>()))
			.ReturnsAsync([..sessionIds]);

	private void SetupQueue(params DiscoverChannelDto[] channels) =>
		storage
			.Setup(s => s.GetChannelsToProcessAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync([..channels]);

	private void SetupResolve(DiscoverChannelDto channel, TelegramOperationResult<TelegramChatInfo> result) =>
		tgMessages
			.Setup(s => s.ResolveChannelAsync(
				It.IsAny<Guid>(), channel.Username!, It.IsAny<CancellationToken>(), It.IsAny<bool>()))
			.ReturnsAsync(result);

	private void SetupResolveThrows(DiscoverChannelDto channel, string message) =>
		tgMessages
			.Setup(s => s.ResolveChannelAsync(
				It.IsAny<Guid>(), channel.Username!, It.IsAny<CancellationToken>(), It.IsAny<bool>()))
			.ThrowsAsync(new InvalidOperationException(message));

	private void VerifyNeverMarkedAsError() =>
		storage.Verify(s => s.MarkAsErrorAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);

	private DiscoverChannelLinksWorker CreateSut()
	{
		var provider = new Mock<IServiceProvider>();
		provider.Setup(p => p.GetService(typeof(ITelegramMessageService))).Returns(tgMessages.Object);
		provider.Setup(p => p.GetService(typeof(ITelegramPublicLookupService))).Returns(publicLookup.Object);

		var scope = new Mock<IServiceScope>();
		scope.Setup(s => s.ServiceProvider).Returns(provider.Object);

		var scopeFactory = new Mock<IServiceScopeFactory>();
		scopeFactory.Setup(f => f.CreateScope()).Returns(scope.Object);

		var lifetime = new Mock<IHostApplicationLifetime>();
		lifetime.Setup(l => l.ApplicationStopping).Returns(CancellationToken.None);

		return new DiscoverChannelLinksWorker(
			storage.Object,
			authService.Object,
			statusStorage.Object,
			scopeFactory.Object,
			new HangfireNextRunProvider(new MemoryStorage()),
			NullLogger<DiscoverChannelLinksWorker>.Instance,
			lifetime.Object);
	}
}
