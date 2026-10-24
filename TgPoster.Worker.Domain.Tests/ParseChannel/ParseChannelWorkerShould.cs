using Hangfire.MemoryStorage;
using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shared.Enums;
using Shouldly;
using TgPoster.Telegram.Abstractions;
using TgPoster.Telegram.Models;
using TgPoster.Worker.Domain.Services.ScheduleAlerts;
using TgPoster.Worker.Domain.UseCases.ParseChannel;
using TgPoster.Worker.Domain.UseCases.ParseChannelWorker;
using TgPoster.Worker.Domain.UseCases.WorkerJobStatus;

namespace TgPoster.Worker.Domain.Tests.ParseChannel;

public sealed class ParseChannelWorkerShould
{
	private const string ChannelName = "source_channel";

	private readonly Mock<IParseChannelWorkerStorage> storage = new();
	private readonly Mock<IParseChannelUseCaseStorage> useCaseStorage = new();
	private readonly Mock<IParseChannelRunStorage> runStorage = new();
	private readonly Mock<IWorkerJobStatusStorage> statusStorage = new();
	private readonly Mock<ITelegramMessageService> tgMessages = new();
	private readonly Mock<IScheduleAlertService> alertService = new();
	private readonly Guid id = Guid.NewGuid();
	private readonly Guid scheduleId = Guid.NewGuid();
	private readonly Guid sessionId = Guid.NewGuid();
	private readonly ParseChannelWorker sut;

	public ParseChannelWorkerShould()
	{
		storage.Setup(s => s.GetChannelParsingParametersAsync()).ReturnsAsync([id]);
		storage.Setup(s => s.GetScheduleIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(scheduleId);
		useCaseStorage.Setup(s => s.GetChannelParsingParametersAsync(id, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new ParseChannelParsingSettings
			{
				ChannelName = ChannelName,
				CheckNewPosts = true,
				TelegramSessionId = sessionId,
				ScheduleId = scheduleId
			});

		var lifetime = new Mock<IHostApplicationLifetime>();
		lifetime.SetupGet(l => l.ApplicationStopping).Returns(CancellationToken.None);

		var useCase = new ParseChannelUseCase(
			useCaseStorage.Object,
			runStorage.Object,
			Mock.Of<IPublishEndpoint>(),
			tgMessages.Object,
			alertService.Object,
			TimeProvider.System,
			NullLogger<ParseChannelUseCase>.Instance);

		sut = new ParseChannelWorker(
			storage.Object,
			useCase,
			runStorage.Object,
			statusStorage.Object,
			new HangfireNextRunProvider(new MemoryStorage()),
			alertService.Object,
			TimeProvider.System,
			NullLogger<ParseChannelWorker>.Instance,
			lifetime.Object);
	}

	[Fact]
	public async Task ReportProgressAndCompletion_WhenSourcesProcessed()
	{
		SetupResolve(TelegramOperationResult<TelegramChatInfo>.Failed(TelegramOperationStatus.UsernameNotFound));

		await sut.ProcessMessagesAsync();

		statusStorage.Verify(s => s.ReportStartedAsync(WorkerJobNames.ParseChannels, It.IsAny<CancellationToken>()),
			Times.Once);
		statusStorage.Verify(s => s.ReportHeartbeatAsync(WorkerJobNames.ParseChannels, 0, 1, null,
			It.IsAny<CancellationToken>()), Times.Once);
		statusStorage.Verify(s => s.ReportCompletedAsync(WorkerJobNames.ParseChannels, It.IsAny<DateTimeOffset?>(),
			It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task ReportJobFailed_WhenSourcesCannotBeLoaded()
	{
		storage.Setup(s => s.GetChannelParsingParametersAsync()).ThrowsAsync(new InvalidOperationException("БД недоступна"));

		await Should.ThrowAsync<InvalidOperationException>(() => sut.ProcessMessagesAsync());

		statusStorage.Verify(s => s.ReportFailedAsync(WorkerJobNames.ParseChannels, "БД недоступна",
			It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()), Times.Once);
		statusStorage.Verify(s => s.ReportCompletedAsync(It.IsAny<string>(), It.IsAny<DateTimeOffset?>(),
			It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task RecordFailedRun_WhenParsingThrows()
	{
		useCaseStorage.Setup(s => s.GetChannelParsingParametersAsync(id, It.IsAny<CancellationToken>()))
			.ThrowsAsync(new InvalidOperationException("БД недоступна"));

		await sut.ProcessMessagesAsync();

		runStorage.Verify(s => s.AddRunAsync(
			It.Is<ParseChannelRunReport>(r => r.ChannelParsingSettingId == id
			                                  && r.Status == ParseChannelRunStatus.Failed
			                                  && r.Error == "БД недоступна"),
			It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task ContinueParsing_WhenRunJournalWriteFails()
	{
		useCaseStorage.Setup(s => s.GetChannelParsingParametersAsync(id, It.IsAny<CancellationToken>()))
			.ThrowsAsync(new InvalidOperationException("БД недоступна"));
		runStorage.Setup(s => s.AddRunAsync(It.IsAny<ParseChannelRunReport>(), It.IsAny<CancellationToken>()))
			.ThrowsAsync(new InvalidOperationException("журнал недоступен"));

		await sut.ProcessMessagesAsync();

		alertService.Verify(a => a.NotifyAsync(scheduleId, ScheduleProblem.ParsingFailed, "БД недоступна", id,
			It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task KeepFailedStatus_WhenSourceUnavailable()
	{
		SetupResolve(TelegramOperationResult<TelegramChatInfo>.Failed(TelegramOperationStatus.UsernameNotFound));

		await sut.ProcessMessagesAsync();

		useCaseStorage.Verify(s => s.UpdateErrorStatusAsync(id, It.IsAny<CancellationToken>()), Times.Once);
		useCaseStorage.Verify(s => s.UpdateChannelParsingParametersAsync(It.IsAny<Guid>(), It.IsAny<int>(),
			It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task LeaveFinalStatusToParsing_WhenParsingSucceeded()
	{
		SetupResolve(TelegramOperationResult<TelegramChatInfo>.Success(new TelegramChatInfo
		{
			Id = 100,
			AccessHash = 1000,
			Title = "Источник",
			Peer = TelegramPeer.Channel(100, 1000)
		}));
		tgMessages.Setup(t => t.GetHistoryAsync(sessionId, It.IsAny<TelegramPeer>(), It.IsAny<int>(), It.IsAny<int>(),
				It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<CancellationToken>(), It.IsAny<bool>()))
			.ReturnsAsync(TelegramOperationResult<TelegramHistoryPage>.Success(new TelegramHistoryPage { Messages = [] }));

		await sut.ProcessMessagesAsync();

		useCaseStorage.Verify(s => s.UpdateChannelParsingParametersAsync(id, 0, true, It.IsAny<CancellationToken>()),
			Times.Once);
		storage.Verify(s => s.SetErrorStatusAsync(It.IsAny<Guid>()), Times.Never);
	}

	[Fact]
	public async Task ReleaseStuckSources_WhenRecoveringAfterRestart()
	{
		storage.Setup(s => s.ReleaseStuckInHandleAsync()).ReturnsAsync(2);

		await sut.RecoverAfterRestartAsync();

		storage.Verify(s => s.ReleaseStuckInHandleAsync(), Times.Once);
	}

	[Fact]
	public async Task MarkFailedAndNotify_WhenParsingThrows()
	{
		useCaseStorage.Setup(s => s.GetChannelParsingParametersAsync(id, It.IsAny<CancellationToken>()))
			.ThrowsAsync(new InvalidOperationException("БД недоступна"));

		await sut.ProcessMessagesAsync();

		storage.Verify(s => s.SetErrorStatusAsync(id), Times.Once);
		alertService.Verify(a => a.NotifyAsync(scheduleId, ScheduleProblem.ParsingFailed, "БД недоступна", id,
			It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task NotNotify_WhenParsingSettingsNotFound()
	{
		useCaseStorage.Setup(s => s.GetChannelParsingParametersAsync(id, It.IsAny<CancellationToken>()))
			.ThrowsAsync(new InvalidOperationException("БД недоступна"));
		storage.Setup(s => s.GetScheduleIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Guid?)null);

		await sut.ProcessMessagesAsync();

		alertService.Verify(a => a.NotifyAsync(It.IsAny<Guid>(), It.IsAny<ScheduleProblem>(), It.IsAny<string>(),
			It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	private void SetupResolve(TelegramOperationResult<TelegramChatInfo> result)
	{
		tgMessages.Setup(t => t.ResolveChannelAsync(sessionId, ChannelName, It.IsAny<CancellationToken>(),
				It.IsAny<bool>()))
			.ReturnsAsync(result);
	}
}
