using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shared.Classification;
using Shared.Enums;
using Shared.OpenRouter;
using Shared.OpenRouter.Models.Request;
using Shared.OpenRouter.Models.Response;
using Shouldly;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using TgPoster.Telegram.Abstractions;
using TgPoster.Telegram.Models;
using TgPoster.Worker.Domain.ConfigModels;
using TgPoster.Worker.Domain.UseCases.ClassifyChannel;
using TgPoster.Worker.Domain.UseCases.WorkerJobStatus;

namespace TgPoster.Worker.Domain.Tests.ClassifyChannel;

public class ClassifyChannelWorkerShould
{
	private static readonly DateTimeOffset FixedNow = new(2026, 9, 26, 12, 0, 0, TimeSpan.Zero);

	private readonly Mock<IOpenRouterClient> openRouter = new();
	private readonly Mock<IClassifyChannelStorage> storage = new();
	private readonly Mock<ITelegramAuthService> authService = new();
	private readonly Mock<ITelegramMessageService> tgMessages = new();
	private readonly Mock<IWorkerJobStatusStorage> statusStorage = new();
	private readonly OpenRouterOptions options = new() { SecretKey = "test-key" };
	private readonly Guid purposeSessionId = Guid.NewGuid();

	private ClassifierSettingsDto settings = new()
	{
		IsEnabled = true,
		Model = "test/model",
		BatchSize = 3,
		IntervalMinutes = 20,
		MessageSampleCount = 10,
		PhotoCount = 0,
		ReclassifyAfterDays = null,
		Categories = ["Альфа", "Бета"],
		SystemPrompt = $"Выбери из: {ClassifierDefaults.CategoriesPlaceholder}"
	};

	public ClassifyChannelWorkerShould()
	{
		storage.Setup(s => s.GetSettingsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(() => settings);
		authService.Setup(s => s.GetSessionIdForPurposeAsync(
				TelegramSessionPurpose.Classification, It.IsAny<CancellationToken>()))
			.ReturnsAsync(purposeSessionId);
	}

	[Fact]
	public async Task SkipRun_WhenDisabled()
	{
		settings = settings with { IsEnabled = false };
		var sut = CreateSut();

		await sut.ClassifyChannelsAsync();

		statusStorage.Verify(s => s.ReportStartedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
			Times.Never);
		VerifyQueueNeverRequested();
	}

	[Fact]
	public async Task SkipRun_WhenIntervalHasNotElapsed()
	{
		SetupLastStartedAt(FixedNow.AddMinutes(-5));
		var sut = CreateSut();

		await sut.ClassifyChannelsAsync();

		statusStorage.Verify(s => s.ReportStartedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
			Times.Never);
		VerifyQueueNeverRequested();
	}

	[Fact]
	public async Task Run_WhenIntervalHasElapsedWithinTolerance()
	{
		SetupLastStartedAt(FixedNow.AddMinutes(-20).AddSeconds(10));
		SetupQueue();
		var sut = CreateSut();

		await sut.ClassifyChannelsAsync();

		statusStorage.Verify(s => s.ReportStartedAsync(WorkerJobNames.ClassifyChannels, It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task ReportNextRun_AfterConfiguredInterval()
	{
		settings = settings with { IntervalMinutes = 45 };
		SetupQueue();
		var sut = CreateSut();

		await sut.ClassifyChannelsAsync();

		statusStorage.Verify(s => s.ReportCompletedAsync(
				WorkerJobNames.ClassifyChannels, FixedNow.AddMinutes(45), It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task UseDefaultSettings_WhenNoneSaved()
	{
		storage.Setup(s => s.GetSettingsAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync((ClassifierSettingsDto?)null);
		SetupQueue();
		var sut = CreateSut();

		await sut.ClassifyChannelsAsync();

		storage.Verify(s => s.GetChannelsToClassifyAsync(
				ClassifierDefaults.BatchSize,
				It.IsAny<DateTimeOffset>(),
				It.IsAny<DateTimeOffset?>(),
				It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task RequestQueue_WithBatchSizeRetryWindowAndNoReclassification()
	{
		SetupQueue();
		var sut = CreateSut();

		await sut.ClassifyChannelsAsync();

		storage.Verify(s => s.GetChannelsToClassifyAsync(
				3, FixedNow.AddHours(-6), null, It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task RequestQueue_WithReclassifyWindow_WhenConfigured()
	{
		settings = settings with { ReclassifyAfterDays = 30 };
		SetupQueue();
		var sut = CreateSut();

		await sut.ClassifyChannelsAsync();

		storage.Verify(s => s.GetChannelsToClassifyAsync(
				3, FixedNow.AddHours(-6), FixedNow.AddDays(-30), It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task ReportFailed_WhenApiKeyIsMissing()
	{
		options.SecretKey = " ";
		var sut = CreateSut();

		await sut.ClassifyChannelsAsync();

		VerifyFailedWith(e => e.Contains("OpenRouter"));
		VerifyQueueNeverRequested();
	}

	[Fact]
	public async Task ReportCompleted_WhenQueueIsEmpty()
	{
		SetupQueue();
		var sut = CreateSut();

		await sut.ClassifyChannelsAsync();

		VerifyCompleted();
		authService.Verify(s => s.GetSessionIdForPurposeAsync(
				It.IsAny<TelegramSessionPurpose>(), It.IsAny<CancellationToken>()),
			Times.Never);
	}

	[Fact]
	public async Task ReportFailed_WhenNoSessionIsAvailable()
	{
		SetupQueue(CreateChannel("first"));
		authService.Setup(s => s.GetSessionIdForPurposeAsync(
				TelegramSessionPurpose.Classification, It.IsAny<CancellationToken>()))
			.ReturnsAsync((Guid?)null);
		var sut = CreateSut();

		await sut.ClassifyChannelsAsync();

		VerifyFailedWith(e => e.Contains("сессия"));
		VerifyNotCompleted();
	}

	[Fact]
	public async Task UseSessionFromSettings_InsteadOfPurposeLookup()
	{
		var settingsSessionId = Guid.NewGuid();
		settings = settings with { TelegramSessionId = settingsSessionId };
		SetupQueue(CreateChannel("first"));
		SetupResolveFails();
		var sut = CreateSut();

		await sut.ClassifyChannelsAsync();

		tgMessages.Verify(s => s.ResolveChannelAsync(
				settingsSessionId, "first", It.IsAny<CancellationToken>(), It.IsAny<bool>()),
			Times.Once);
		authService.Verify(s => s.GetSessionIdForPurposeAsync(
				It.IsAny<TelegramSessionPurpose>(), It.IsAny<CancellationToken>()),
			Times.Never);
	}

	[Fact]
	public async Task MarkAttemptAndReportHeartbeat_BeforeEachChannel()
	{
		var first = CreateChannel("first");
		var second = CreateChannel("second");
		SetupQueue(first, second);
		SetupResolveFails();
		var sut = CreateSut();

		await sut.ClassifyChannelsAsync();

		statusStorage.Verify(s => s.ReportHeartbeatAsync(
				WorkerJobNames.ClassifyChannels, 0, 2, "@first", It.IsAny<CancellationToken>()),
			Times.Once);
		statusStorage.Verify(s => s.ReportHeartbeatAsync(
				WorkerJobNames.ClassifyChannels, 1, 2, "@second", It.IsAny<CancellationToken>()),
			Times.Once);
		storage.Verify(s => s.MarkClassificationAttemptAsync(first.Id, FixedNow, It.IsAny<CancellationToken>()),
			Times.Once);
		storage.Verify(s => s.MarkClassificationAttemptAsync(second.Id, FixedNow, It.IsAny<CancellationToken>()),
			Times.Once);
		VerifyCompleted();
	}

	[Fact]
	public async Task ReportFailedWithLastError_WhenEveryChannelThrows()
	{
		SetupQueue(CreateChannel("first"), CreateChannel("second"));
		tgMessages.Setup(s => s.ResolveChannelAsync(
				It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<bool>()))
			.ThrowsAsync(new InvalidOperationException("boom"));
		var sut = CreateSut();

		await sut.ClassifyChannelsAsync();

		VerifyFailedWith(e => e == "boom");
		VerifyNotCompleted();
	}

	[Fact]
	public async Task ReportCompleted_WhenOnlySomeChannelsThrow()
	{
		SetupQueue(CreateChannel("broken"), CreateChannel("fine"));
		SetupResolveFails();
		tgMessages.Setup(s => s.ResolveChannelAsync(
				It.IsAny<Guid>(), "broken", It.IsAny<CancellationToken>(), It.IsAny<bool>()))
			.ThrowsAsync(new InvalidOperationException("boom"));
		var sut = CreateSut();

		await sut.ClassifyChannelsAsync();

		VerifyCompleted();
		statusStorage.Verify(s => s.ReportFailedAsync(
				It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()),
			Times.Never);
	}

	[Fact]
	public async Task ReportFailedAndRethrow_WhenStorageThrows()
	{
		storage.Setup(s => s.GetChannelsToClassifyAsync(
				It.IsAny<int>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()))
			.ThrowsAsync(new InvalidOperationException("db is down"));
		var sut = CreateSut();

		await Should.ThrowAsync<InvalidOperationException>(() => sut.ClassifyChannelsAsync());

		VerifyFailedWith(e => e == "db is down");
	}

	[Fact]
	public async Task KeepWorking_WhenStatusStorageThrows()
	{
		statusStorage.Setup(s => s.ReportStartedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.ThrowsAsync(new InvalidOperationException("status table is locked"));
		SetupQueue();
		var sut = CreateSut();

		await sut.ClassifyChannelsAsync();

		VerifyCompleted();
	}

	[Fact]
	public async Task SendSettingsModelAndPromptWithCategories_AndSaveResult()
	{
		var channel = CreateChannel("first");
		SetupQueue(channel);
		SetupResolveSucceeds();
		SetupHistory(new TelegramMessage { Id = 1, Date = DateTime.UtcNow, Text = "Пост про альфу" });
		List<ChatMessage>? sent = null;
		string? sentModel = null;
		openRouter.Setup(s => s.SendMessageRawAsync(
				It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<ChatMessage>>(), It.IsAny<CancellationToken>()))
			.Callback<string, string, List<ChatMessage>, CancellationToken>((_, model, messages, _) =>
			{
				sentModel = model;
				sent = messages;
			})
			.ReturnsAsync(LlmAnswer("""{"category":"Альфа","subcategory":"Под","tags":["t1"],"language":"ru","confidence":0.9}"""));
		var sut = CreateSut();

		await sut.ClassifyChannelsAsync();

		sentModel.ShouldBe("test/model");
		sent.ShouldNotBeNull();
		sent[0].Content.ShouldBe("Выбери из: Альфа, Бета");
		storage.Verify(s => s.UpdateClassificationAsync(
				channel.Id, "Альфа", "Под", It.Is<string[]>(t => t.SequenceEqual(new[] { "t1" })), "ru", 0.9,
				It.IsAny<CancellationToken>()),
			Times.Once);
		tgMessages.Verify(s => s.GetHistoryAsync(
				It.IsAny<Guid>(), It.IsAny<TelegramPeer>(), 10, It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<int>(),
				It.IsAny<CancellationToken>(), It.IsAny<bool>()),
			Times.Once);
	}

	[Fact]
	public async Task AttachPhotos_UpToConfiguredCount()
	{
		settings = settings with { PhotoCount = 2 };
		SetupQueue(CreateChannel("first"));
		SetupResolveSucceeds();
		SetupHistory(
			PhotoMessage(1),
			PhotoMessage(2),
			PhotoMessage(3));
		tgMessages.Setup(s => s.DownloadMediaAsync(
				It.IsAny<Guid>(), It.IsAny<TelegramPeer>(), It.IsAny<int>(), It.IsAny<TelegramMessageMedia>(),
				It.IsAny<Stream>(), It.IsAny<CancellationToken>(), It.IsAny<bool>()))
			.Callback<Guid, TelegramPeer, int, TelegramMessageMedia, Stream, CancellationToken, bool>(
				(_, _, _, _, target, _, _) => WriteTinyJpeg(target))
			.ReturnsAsync(TelegramOperationResult.Success());
		openRouter.Setup(s => s.ToLocalImageDataUrl(It.IsAny<byte[]>())).Returns("data:image/jpeg;base64,AAA");
		List<ChatMessage>? sent = null;
		openRouter.Setup(s => s.SendMessageRawAsync(
				It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<ChatMessage>>(), It.IsAny<CancellationToken>()))
			.Callback<string, string, List<ChatMessage>, CancellationToken>((_, _, messages, _) => sent = messages)
			.ReturnsAsync(LlmAnswer("""{"category":"Альфа","confidence":0.9}"""));
		var sut = CreateSut();

		await sut.ClassifyChannelsAsync();

		var parts = sent.ShouldNotBeNull()[1].Content.ShouldBeOfType<List<MessageContentPart>>();
		parts.Count(p => p.Type == "image_url").ShouldBe(2);
		tgMessages.Verify(s => s.DownloadMediaAsync(
				It.IsAny<Guid>(), It.IsAny<TelegramPeer>(), It.IsAny<int>(), It.IsAny<TelegramMessageMedia>(),
				It.IsAny<Stream>(), It.IsAny<CancellationToken>(), It.IsAny<bool>()),
			Times.Exactly(2));
	}

	[Fact]
	public async Task NotDownloadPhotos_WhenPhotoCountIsZero()
	{
		SetupQueue(CreateChannel("first"));
		SetupResolveSucceeds();
		SetupHistory(PhotoMessage(1));
		openRouter.Setup(s => s.SendMessageRawAsync(
				It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<ChatMessage>>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(LlmAnswer("""{"category":"Альфа","confidence":0.9}"""));
		var sut = CreateSut();

		await sut.ClassifyChannelsAsync();

		tgMessages.Verify(s => s.DownloadMediaAsync(
				It.IsAny<Guid>(), It.IsAny<TelegramPeer>(), It.IsAny<int>(), It.IsAny<TelegramMessageMedia>(),
				It.IsAny<Stream>(), It.IsAny<CancellationToken>(), It.IsAny<bool>()),
			Times.Never);
	}

	[Fact]
	public void CreateDefaultSettings_TakeModelAndSampleFromConfig()
	{
		var result = ClassifyChannelWorker.CreateDefaultSettings(
			new OpenRouterOptions { Model = "config/model", MessageSampleCount = 40 });

		result.Model.ShouldBe("config/model");
		result.MessageSampleCount.ShouldBe(40);
		result.BatchSize.ShouldBe(ClassifierDefaults.BatchSize);
		result.Categories.ShouldBe(ClassifierDefaults.Categories);
		result.SystemPrompt.ShouldContain(ClassifierDefaults.CategoriesPlaceholder);
		result.ReclassifyAfterDays.ShouldBeNull();
	}

	private void SetupLastStartedAt(DateTimeOffset? value) =>
		statusStorage.Setup(s => s.GetLastStartedAtAsync(WorkerJobNames.ClassifyChannels, It.IsAny<CancellationToken>()))
			.ReturnsAsync(value);

	private void SetupQueue(params ChannelForClassificationDto[] channels) =>
		storage.Setup(s => s.GetChannelsToClassifyAsync(
				It.IsAny<int>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync([..channels]);

	private void SetupResolveFails() =>
		tgMessages.Setup(s => s.ResolveChannelAsync(
				It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<bool>()))
			.ReturnsAsync(TelegramOperationResult<TelegramChatInfo>.Failed(TelegramOperationStatus.Timeout));

	private void SetupResolveSucceeds() =>
		tgMessages.Setup(s => s.ResolveChannelAsync(
				It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<bool>()))
			.ReturnsAsync(TelegramOperationResult<TelegramChatInfo>.Success(new TelegramChatInfo
			{
				Id = 1,
				AccessHash = 2,
				Title = "Channel",
				Peer = TelegramPeer.Channel(1, 2)
			}));

	private void SetupHistory(params TelegramMessage[] messages) =>
		tgMessages.Setup(s => s.GetHistoryAsync(
				It.IsAny<Guid>(), It.IsAny<TelegramPeer>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>(),
				It.IsAny<int>(), It.IsAny<CancellationToken>(), It.IsAny<bool>()))
			.ReturnsAsync(TelegramOperationResult<TelegramHistoryPage>.Success(new TelegramHistoryPage
			{
				Messages = messages
			}));

	private void VerifyCompleted() =>
		statusStorage.Verify(s => s.ReportCompletedAsync(
				WorkerJobNames.ClassifyChannels, It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()),
			Times.Once);

	private void VerifyNotCompleted() =>
		statusStorage.Verify(s => s.ReportCompletedAsync(
				It.IsAny<string>(), It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()),
			Times.Never);

	private void VerifyFailedWith(Func<string, bool> error) =>
		statusStorage.Verify(s => s.ReportFailedAsync(
				WorkerJobNames.ClassifyChannels,
				It.Is<string>(e => error(e)),
				It.IsAny<DateTimeOffset?>(),
				It.IsAny<CancellationToken>()),
			Times.Once);

	private void VerifyQueueNeverRequested() =>
		storage.Verify(s => s.GetChannelsToClassifyAsync(
				It.IsAny<int>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()),
			Times.Never);

	private static ChannelForClassificationDto CreateChannel(string username) => new()
	{
		Id = Guid.NewGuid(),
		Title = username,
		Description = null,
		Username = username,
		TelegramId = null
	};

	private static TelegramMessage PhotoMessage(int id) => new()
	{
		Id = id,
		Date = DateTime.UtcNow,
		Media = new TelegramMessageMedia { Type = TelegramMediaType.Photo }
	};

	private static void WriteTinyJpeg(Stream target)
	{
		using var image = new Image<Rgba32>(4, 4);
		image.SaveAsJpeg(target);
	}

	private static ChatCompletionResponse LlmAnswer(string json) => new()
	{
		Id = "1",
		Model = "test/model",
		Usage = new UsageStats(),
		Choices =
		[
			new Choice
			{
				Message = new ChatMessage { Role = "assistant", Content = json },
				FinishReason = "stop"
			}
		]
	};

	private ClassifyChannelWorker CreateSut() => new(
		openRouter.Object,
		storage.Object,
		authService.Object,
		tgMessages.Object,
		statusStorage.Object,
		new FixedTimeProvider(FixedNow),
		options,
		NullLogger<ClassifyChannelWorker>.Instance,
		new FakeLifetime());

	private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
	{
		public override DateTimeOffset GetUtcNow() => now;
	}

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
