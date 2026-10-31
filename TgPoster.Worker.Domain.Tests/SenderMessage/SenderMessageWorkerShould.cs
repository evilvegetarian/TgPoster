using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Security.Cryptography;
using Shared.Telegram;
using Shared.YouTube;
using Shouldly;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TgPoster.Worker.Domain.ConfigModels;
using TgPoster.Worker.Domain.Services.ScheduleAlerts;
using TgPoster.Worker.Domain.UseCases;
using TgPoster.Worker.Domain.UseCases.RepostMessageConsumer;
using TgPoster.Worker.Domain.UseCases.SenderMessageWorker;

namespace TgPoster.Worker.Domain.Tests.SenderMessage;

public sealed class SenderMessageWorkerShould : IDisposable
{
	private readonly Mock<ISenderMessageStorage> storage = new();
	private readonly Mock<IPublishEndpoint> publisher = new();
	private readonly Mock<IScheduleAlertService> alerts = new();
	private readonly Mock<TelegramExecuteServices> telegram = new(NullLogger<TelegramExecuteServices>.Instance);
	private readonly CancellationTokenSource stopping = new();
	private readonly TelegramBotManager botManager = new();
	private readonly MessageDto post = new()
	{
		Id = Guid.NewGuid(),
		ScheduleId = Guid.NewGuid(),
		TimePosting = new DateTimeOffset(2026, 10, 24, 15, 0, 0, TimeSpan.Zero),
		Message = "Актуальный текст"
	};
	private readonly SenderMessageWorker sut;

	public SenderMessageWorkerShould()
	{
		var crypto = new Mock<ICryptoAES>();
		crypto.Setup(x => x.Decrypt("key", "encrypted"))
			.Returns("123456:ABC-DEF1234ghIkl-zyx57W2v1u123ew11");
		var lifetime = new Mock<IHostApplicationLifetime>();
		lifetime.SetupGet(x => x.ApplicationStopping).Returns(() => stopping.Token);
		storage.Setup(x => x.TakeMessageForSendingAsync(post.Id, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new MessageDetail { Api = "encrypted", ChannelId = 100, MessageDto = [post] });
		storage.Setup(x => x.GetRepostSettingsForMessageAsync(post.Id, It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);
		publisher.Setup(x => x.Publish(It.IsAny<RepostMessageCommand>(), It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);
		telegram.Setup(x => x.SendTextAsync(It.IsAny<ITelegramBotClient>(), 100, It.IsAny<string>(),
				It.IsAny<CancellationToken>(), ParseMode.Html))
			.ReturnsAsync(TelegramSendResult.Success([42]));
		telegram.Setup(x => x.SendMediaGroupAsync(It.IsAny<ITelegramBotClient>(), 100,
				It.IsAny<IEnumerable<IAlbumInputMedia>>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(TelegramSendResult.Success([10, 11]));
		sut = new SenderMessageWorker(storage.Object, NullLogger<SenderMessageWorker>.Instance,
			crypto.Object, new TelegramOptions { SecretKey = "key", TelegramSessionId = Guid.Empty }, new YouTubeService(),
			publisher.Object, botManager, lifetime.Object, telegram.Object, alerts.Object);
	}

	public void Dispose()
	{
		stopping.Dispose();
		botManager.Dispose();
	}

	[Fact]
	public async Task ReadCurrentPost_WhenExecutingPreviouslyScheduledJob()
	{
		var oldSnapshot = new MessageDto { Id = post.Id, Message = "Старый текст" };

		await sut.SendMessageAsync(post.Id, "old-token", 999, oldSnapshot, null);

		storage.Verify(x => x.TakeMessageForSendingAsync(post.Id, stopping.Token), Times.Once);
		telegram.Verify(x => x.SendTextAsync(It.IsAny<ITelegramBotClient>(), 100,
			"Актуальный текст", stopping.Token, ParseMode.Html), Times.Once);
	}

	[Fact]
	public async Task SendTextAndSaveTelegramIds_WhenPostIsReady()
	{
		post.Signature = "Подпись";

		await sut.SendMessageAsync(post.Id);

		telegram.Verify(x => x.SendTextAsync(It.IsAny<ITelegramBotClient>(), 100,
			"Актуальный текст\n\nПодпись", stopping.Token, ParseMode.Html), Times.Once);
		storage.Verify(x => x.SaveTelegramMessageIdsAsync(post.Id, 42,
			It.Is<int[]>(ids => ids.SequenceEqual(new[] { 42 })), 42, stopping.Token), Times.Once);
		storage.Verify(x => x.UpdateSendStatusMessageAsync(It.IsAny<Guid>()), Times.Never);
	}

	[Fact]
	public async Task SaveAlbumAndSeparateTextIds_WhenCaptionIsTooLong()
	{
		post.Message = new string('а', 1100);
		post.File = [new FileDto { TgFileId = "photo-1", ContentType = "image/jpeg" },
			new FileDto { TgFileId = "photo-2", ContentType = "image/jpeg" }];

		await sut.SendMessageAsync(post.Id);

		telegram.Verify(x => x.SendMediaGroupAsync(It.IsAny<ITelegramBotClient>(), 100,
			It.Is<IEnumerable<IAlbumInputMedia>>(items => items.Count() == 2), stopping.Token), Times.Once);
		storage.Verify(x => x.SaveTelegramMessageIdsAsync(post.Id, 10,
			It.Is<int[]>(ids => ids.SequenceEqual(new[] { 10, 11, 42 })), 42, stopping.Token), Times.Once);
	}

	[Theory]
	[InlineData(false, ScheduleProblem.PostFailed)]
	[InlineData(true, ScheduleProblem.BotLostAccess)]
	public async Task SaveErrorAndNotifyWithoutRepost_WhenTelegramRejectsPost(bool accessLost, ScheduleProblem problem)
	{
		telegram.Setup(x => x.SendTextAsync(It.IsAny<ITelegramBotClient>(), 100, It.IsAny<string>(),
				It.IsAny<CancellationToken>(), ParseMode.Html))
			.ReturnsAsync(TelegramSendResult.Failure("403: Forbidden", accessLost));

		await sut.SendMessageAsync(post.Id);

		storage.Verify(x => x.UpdateErrorStatusMessageAsync(post.Id, "403: Forbidden", stopping.Token), Times.Once);
		alerts.Verify(x => x.NotifyAsync(post.ScheduleId, problem,
			It.Is<string>(text => text.Contains("403: Forbidden")), null, stopping.Token), Times.Once);
		storage.Verify(x => x.SaveTelegramMessageIdsAsync(It.IsAny<Guid>(), It.IsAny<int>(),
			It.IsAny<int[]>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Never);
		publisher.Verify(x => x.Publish(It.IsAny<RepostMessageCommand>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task PublishRepostsAfterSavingTelegramIds_WhenDestinationsExist()
	{
		var active = new RepostSettingsDto
		{
			Id = Guid.NewGuid(), ScheduleId = post.ScheduleId, TelegramSessionId = Guid.NewGuid(),
			Destinations = [new RepostDestinationDto { Id = Guid.NewGuid(), ChatIdentifier = 200 }]
		};
		var empty = new RepostSettingsDto
		{
			Id = Guid.NewGuid(), ScheduleId = post.ScheduleId, TelegramSessionId = Guid.NewGuid()
		};
		storage.Setup(x => x.GetRepostSettingsForMessageAsync(post.Id, It.IsAny<CancellationToken>()))
			.ReturnsAsync([active, empty]);
		var saved = false;
		storage.Setup(x => x.SaveTelegramMessageIdsAsync(post.Id, 42, It.IsAny<int[]>(), 42,
				It.IsAny<CancellationToken>()))
			.Callback(() => saved = true).Returns(Task.CompletedTask);
		publisher.Setup(x => x.Publish(It.IsAny<RepostMessageCommand>(), It.IsAny<CancellationToken>()))
			.Callback(() => saved.ShouldBeTrue()).Returns(Task.CompletedTask);

		await sut.SendMessageAsync(post.Id);

		publisher.Verify(x => x.Publish(It.Is<RepostMessageCommand>(command =>
			command.MessageId == post.Id && command.ScheduleId == post.ScheduleId && command.RepostSettingsId == active.Id),
			stopping.Token), Times.Once);
		publisher.Verify(x => x.Publish(It.IsAny<RepostMessageCommand>(), It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task NotSend_WhenStorageDoesNotGrantPost()
	{
		storage.Setup(x => x.TakeMessageForSendingAsync(post.Id, It.IsAny<CancellationToken>()))
			.ReturnsAsync((MessageDetail?)null);

		await sut.SendMessageAsync(post.Id);

		telegram.Verify(x => x.SendTextAsync(It.IsAny<ITelegramBotClient>(), It.IsAny<long>(), It.IsAny<string>(),
			It.IsAny<CancellationToken>(), It.IsAny<ParseMode>()), Times.Never);
		publisher.Verify(x => x.Publish(It.IsAny<RepostMessageCommand>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task NotTakePost_WhenShutdownHasAlreadyStarted()
	{
		await stopping.CancelAsync();

		await Should.ThrowAsync<OperationCanceledException>(() => sut.SendMessageAsync(post.Id));

		storage.Verify(x => x.TakeMessageForSendingAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task NotSaveSuccessOrPublish_WhenSendingIsCanceled()
	{
		telegram.Setup(x => x.SendTextAsync(It.IsAny<ITelegramBotClient>(), 100, It.IsAny<string>(),
				It.IsAny<CancellationToken>(), ParseMode.Html))
			.ThrowsAsync(new OperationCanceledException(stopping.Token));

		await Should.ThrowAsync<OperationCanceledException>(() => sut.SendMessageAsync(post.Id));

		storage.Verify(x => x.SaveTelegramMessageIdsAsync(It.IsAny<Guid>(), It.IsAny<int>(),
			It.IsAny<int[]>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Never);
		storage.Verify(x => x.UpdateErrorStatusMessageAsync(It.IsAny<Guid>(), It.IsAny<string>(),
			It.IsAny<CancellationToken>()), Times.Never);
		publisher.Verify(x => x.Publish(It.IsAny<RepostMessageCommand>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task SendOnlyGrantedPost_WhenTwoHandlersRunTogether()
	{
		var taken = 0;
		storage.Setup(x => x.TakeMessageForSendingAsync(post.Id, It.IsAny<CancellationToken>()))
			.ReturnsAsync(() => Interlocked.Increment(ref taken) == 1
				? new MessageDetail { Api = "encrypted", ChannelId = 100, MessageDto = [post] } : null);
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		telegram.Setup(x => x.SendTextAsync(It.IsAny<ITelegramBotClient>(), 100, It.IsAny<string>(),
				It.IsAny<CancellationToken>(), ParseMode.Html))
			.Returns(async () =>
			{
				entered.SetResult();
				await release.Task;
				return TelegramSendResult.Success([42]);
			});

		var first = sut.SendMessageAsync(post.Id);
		await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
		await sut.SendMessageAsync(post.Id);
		release.SetResult();
		await first;

		telegram.Verify(x => x.SendTextAsync(It.IsAny<ITelegramBotClient>(), 100, It.IsAny<string>(),
			It.IsAny<CancellationToken>(), ParseMode.Html), Times.Once);
	}

	[Fact]
	public async Task NotSendAgain_WhenSavingResultFailsAfterTelegramSuccess()
	{
		storage.SetupSequence(x => x.TakeMessageForSendingAsync(post.Id, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new MessageDetail { Api = "encrypted", ChannelId = 100, MessageDto = [post] })
			.ReturnsAsync((MessageDetail?)null);
		storage.Setup(x => x.SaveTelegramMessageIdsAsync(post.Id, 42, It.IsAny<int[]>(), 42,
				It.IsAny<CancellationToken>()))
			.ThrowsAsync(new InvalidOperationException("storage unavailable"));

		await Should.ThrowAsync<InvalidOperationException>(() => sut.SendMessageAsync(post.Id));
		await sut.SendMessageAsync(post.Id);

		telegram.Verify(x => x.SendTextAsync(It.IsAny<ITelegramBotClient>(), 100, It.IsAny<string>(),
			It.IsAny<CancellationToken>(), ParseMode.Html), Times.Once);
		publisher.Verify(x => x.Publish(It.IsAny<RepostMessageCommand>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task NotResendPost_WhenPublishingRepostCommandFails()
	{
		storage.SetupSequence(x => x.TakeMessageForSendingAsync(post.Id, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new MessageDetail { Api = "encrypted", ChannelId = 100, MessageDto = [post] })
			.ReturnsAsync((MessageDetail?)null);
		storage.Setup(x => x.GetRepostSettingsForMessageAsync(post.Id, It.IsAny<CancellationToken>()))
			.ReturnsAsync([new RepostSettingsDto
			{
				Id = Guid.NewGuid(), ScheduleId = post.ScheduleId, TelegramSessionId = Guid.NewGuid(),
				Destinations = [new RepostDestinationDto { Id = Guid.NewGuid(), ChatIdentifier = 200 }]
			}]);
		publisher.Setup(x => x.Publish(It.IsAny<RepostMessageCommand>(), It.IsAny<CancellationToken>()))
			.ThrowsAsync(new InvalidOperationException("queue unavailable"));

		await Should.ThrowAsync<InvalidOperationException>(() => sut.SendMessageAsync(post.Id));
		await sut.SendMessageAsync(post.Id);

		telegram.Verify(x => x.SendTextAsync(It.IsAny<ITelegramBotClient>(), 100, It.IsAny<string>(),
			It.IsAny<CancellationToken>(), ParseMode.Html), Times.Once);
		storage.Verify(x => x.SaveTelegramMessageIdsAsync(post.Id, 42, It.IsAny<int[]>(), 42,
			It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public void BuildPostFailedDetails_WithUtcTimeErrorAndPlainTextPreview()
	{
		var message = new MessageDto
		{
			Id = Guid.NewGuid(),
			TimePosting = new DateTimeOffset(2026, 10, 24, 18, 5, 0, TimeSpan.FromHours(3)),
			Message = "<b>Заголовок</b> &amp; текст"
		};

		var result = SenderMessageWorker.BuildPostFailedDetails(message, "400: Bad Request: message is too long");

		result.ShouldBe("Пост на 24.10 15:05 UTC не отправлен: 400: Bad Request: message is too long\nТекст: Заголовок & текст");
	}

	[Fact]
	public void BuildPostFailedDetails_WithTruncatedPreview_WhenTextLong()
	{
		var message = new MessageDto
		{
			Id = Guid.NewGuid(),
			TimePosting = new DateTimeOffset(2026, 10, 24, 15, 5, 0, TimeSpan.Zero),
			Message = new string('а', 200)
		};

		var result = SenderMessageWorker.BuildPostFailedDetails(message, "ошибка");

		var preview = result.Split('\n')[1];
		preview.ShouldBe("Текст: " + new string('а', 79) + "…");
	}

	[Fact]
	public void BuildPostFailedDetails_WithoutPreview_WhenPostHasNoText()
	{
		var message = new MessageDto
		{
			Id = Guid.NewGuid(),
			TimePosting = new DateTimeOffset(2026, 10, 24, 15, 5, 0, TimeSpan.Zero)
		};

		var result = SenderMessageWorker.BuildPostFailedDetails(message, "ошибка");

		result.ShouldBe("Пост на 24.10 15:05 UTC не отправлен: ошибка");
	}

	[Fact]
	public void BuildSentMessageIds_WithFirstAlbumMessageAsTextHolder_WhenTextIsCaption()
	{
		var result = SenderMessageWorker.BuildSentMessageIds([10, 11, 12], null);

		result.ShouldNotBeNull();
		result.FirstId.ShouldBe(10);
		result.AllIds.ShouldBe([10, 11, 12]);
		result.TextMessageId.ShouldBe(10);
	}

	[Fact]
	public void BuildSentMessageIds_WithSeparateTextAsHolder_WhenTextSentSeparately()
	{
		var result = SenderMessageWorker.BuildSentMessageIds([10, 11], 12);

		result.ShouldNotBeNull();
		result.FirstId.ShouldBe(10);
		result.AllIds.ShouldBe([10, 11, 12]);
		result.TextMessageId.ShouldBe(12);
	}

	[Fact]
	public void BuildSentMessageIds_WithPostAsTextHolder_WhenTextPost()
	{
		var result = SenderMessageWorker.BuildSentMessageIds([42], null);

		result.ShouldNotBeNull();
		result.FirstId.ShouldBe(42);
		result.AllIds.ShouldBe([42]);
		result.TextMessageId.ShouldBe(42);
	}

	[Fact]
	public void BuildSentMessageIds_ReturnNull_WhenTelegramReturnedNoIds()
	{
		var result = SenderMessageWorker.BuildSentMessageIds([], 12);

		result.ShouldBeNull();
	}
}
