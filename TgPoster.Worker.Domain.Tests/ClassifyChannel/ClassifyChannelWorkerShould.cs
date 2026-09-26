using Hangfire.MemoryStorage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shared.Enums;
using Shouldly;
using Shared.OpenRouter;
using TgPoster.Telegram.Abstractions;
using TgPoster.Telegram.Models;
using TgPoster.Worker.Domain.ConfigModels;
using TgPoster.Worker.Domain.UseCases.ClassifyChannel;
using TgPoster.Worker.Domain.UseCases.WorkerJobStatus;

namespace TgPoster.Worker.Domain.Tests.ClassifyChannel;

public class ClassifyChannelWorkerShould
{
	private readonly Mock<IClassifyChannelStorage> storage = new();
	private readonly Mock<ITelegramAuthService> authService = new();
	private readonly Mock<ITelegramMessageService> tgMessages = new();
	private readonly Mock<IWorkerJobStatusStorage> statusStorage = new();
	private readonly OpenRouterOptions options = new() { SecretKey = "test-key" };
	private readonly Guid sessionId = Guid.NewGuid();

	public ClassifyChannelWorkerShould()
	{
		authService.Setup(s => s.GetSessionIdForPurposeAsync(
				TelegramSessionPurpose.Classification, It.IsAny<CancellationToken>()))
			.ReturnsAsync(sessionId);
	}

	[Fact]
	public async Task ReportFailed_WhenApiKeyIsMissing()
	{
		options.SecretKey = " ";
		var sut = CreateSut();

		await sut.ClassifyChannelsAsync();

		statusStorage.Verify(s => s.ReportStartedAsync(WorkerJobNames.ClassifyChannels, It.IsAny<CancellationToken>()),
			Times.Once);
		statusStorage.Verify(s => s.ReportFailedAsync(
				WorkerJobNames.ClassifyChannels,
				It.Is<string>(e => e.Contains("OpenRouter")),
				It.IsAny<DateTimeOffset?>(),
				It.IsAny<CancellationToken>()),
			Times.Once);
		storage.Verify(s => s.GetUnclassifiedChannelsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
			Times.Never);
	}

	[Fact]
	public async Task ReportCompleted_WhenQueueIsEmpty()
	{
		storage.Setup(s => s.GetUnclassifiedChannelsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);
		var sut = CreateSut();

		await sut.ClassifyChannelsAsync();

		VerifyCompleted();
		authService.Verify(s => s.GetSessionIdForPurposeAsync(
				It.IsAny<TelegramSessionPurpose>(), It.IsAny<CancellationToken>()),
			Times.Never);
	}

	[Fact]
	public async Task ReportFailed_WhenClassificationSessionIsMissing()
	{
		storage.Setup(s => s.GetUnclassifiedChannelsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync([CreateChannel("first")]);
		authService.Setup(s => s.GetSessionIdForPurposeAsync(
				TelegramSessionPurpose.Classification, It.IsAny<CancellationToken>()))
			.ReturnsAsync((Guid?)null);
		var sut = CreateSut();

		await sut.ClassifyChannelsAsync();

		statusStorage.Verify(s => s.ReportFailedAsync(
				WorkerJobNames.ClassifyChannels,
				It.Is<string>(e => e.Contains("сессии")),
				It.IsAny<DateTimeOffset?>(),
				It.IsAny<CancellationToken>()),
			Times.Once);
		VerifyNotCompleted();
	}

	[Fact]
	public async Task ReportHeartbeatWithProgress_BeforeEachChannel()
	{
		storage.Setup(s => s.GetUnclassifiedChannelsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync([CreateChannel("first"), CreateChannel("second")]);
		SetupResolveFails();
		var sut = CreateSut();

		await sut.ClassifyChannelsAsync();

		statusStorage.Verify(s => s.ReportHeartbeatAsync(
				WorkerJobNames.ClassifyChannels, 0, 2, "@first", It.IsAny<CancellationToken>()),
			Times.Once);
		statusStorage.Verify(s => s.ReportHeartbeatAsync(
				WorkerJobNames.ClassifyChannels, 1, 2, "@second", It.IsAny<CancellationToken>()),
			Times.Once);
		VerifyCompleted();
	}

	[Fact]
	public async Task ReportFailedWithLastError_WhenEveryChannelThrows()
	{
		storage.Setup(s => s.GetUnclassifiedChannelsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync([CreateChannel("first"), CreateChannel("second")]);
		tgMessages.Setup(s => s.ResolveChannelAsync(
				sessionId, It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<bool>()))
			.ThrowsAsync(new InvalidOperationException("boom"));
		var sut = CreateSut();

		await sut.ClassifyChannelsAsync();

		statusStorage.Verify(s => s.ReportFailedAsync(
				WorkerJobNames.ClassifyChannels,
				"boom",
				It.IsAny<DateTimeOffset?>(),
				It.IsAny<CancellationToken>()),
			Times.Once);
		VerifyNotCompleted();
	}

	[Fact]
	public async Task ReportCompleted_WhenOnlySomeChannelsThrow()
	{
		storage.Setup(s => s.GetUnclassifiedChannelsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync([CreateChannel("broken"), CreateChannel("fine")]);
		SetupResolveFails();
		tgMessages.Setup(s => s.ResolveChannelAsync(
				sessionId, "broken", It.IsAny<CancellationToken>(), It.IsAny<bool>()))
			.ThrowsAsync(new InvalidOperationException("boom"));
		var sut = CreateSut();

		await sut.ClassifyChannelsAsync();

		VerifyCompleted();
		statusStorage.Verify(s => s.ReportFailedAsync(
				It.IsAny<string>(),
				It.IsAny<string>(),
				It.IsAny<DateTimeOffset?>(),
				It.IsAny<CancellationToken>()),
			Times.Never);
	}

	[Fact]
	public async Task ReportFailedAndRethrow_WhenStorageThrows()
	{
		storage.Setup(s => s.GetUnclassifiedChannelsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
			.ThrowsAsync(new InvalidOperationException("db is down"));
		var sut = CreateSut();

		await Should.ThrowAsync<InvalidOperationException>(() => sut.ClassifyChannelsAsync());

		statusStorage.Verify(s => s.ReportFailedAsync(
				WorkerJobNames.ClassifyChannels,
				"db is down",
				It.IsAny<DateTimeOffset?>(),
				It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task KeepWorking_WhenStatusStorageThrows()
	{
		statusStorage.Setup(s => s.ReportStartedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.ThrowsAsync(new InvalidOperationException("status table is locked"));
		storage.Setup(s => s.GetUnclassifiedChannelsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);
		var sut = CreateSut();

		await sut.ClassifyChannelsAsync();

		storage.Verify(s => s.GetUnclassifiedChannelsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
			Times.Once);
		VerifyCompleted();
	}

	private void SetupResolveFails() =>
		tgMessages.Setup(s => s.ResolveChannelAsync(
				sessionId, It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<bool>()))
			.ReturnsAsync(TelegramOperationResult<TelegramChatInfo>.Failed(TelegramOperationStatus.Timeout));

	private void VerifyCompleted() =>
		statusStorage.Verify(s => s.ReportCompletedAsync(
				WorkerJobNames.ClassifyChannels,
				It.IsAny<DateTimeOffset?>(),
				It.IsAny<CancellationToken>()),
			Times.Once);

	private void VerifyNotCompleted() =>
		statusStorage.Verify(s => s.ReportCompletedAsync(
				It.IsAny<string>(),
				It.IsAny<DateTimeOffset?>(),
				It.IsAny<CancellationToken>()),
			Times.Never);

	private static ChannelForClassificationDto CreateChannel(string username) => new()
	{
		Id = Guid.NewGuid(),
		Title = username,
		Description = null,
		Username = username,
		TelegramId = null
	};

	private ClassifyChannelWorker CreateSut() => new(
		Mock.Of<IOpenRouterClient>(),
		storage.Object,
		authService.Object,
		tgMessages.Object,
		statusStorage.Object,
		new HangfireNextRunProvider(new MemoryStorage()),
		options,
		NullLogger<ClassifyChannelWorker>.Instance,
		new FakeLifetime());

	private sealed class FakeLifetime : IHostApplicationLifetime
	{
		public CancellationToken ApplicationStarted => CancellationToken.None;
		public CancellationToken ApplicationStopping => CancellationToken.None;
		public CancellationToken ApplicationStopped => CancellationToken.None;

		public void StopApplication()
		{
		}
	}
}
