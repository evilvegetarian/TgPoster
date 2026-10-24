using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using TgPoster.Telegram.Abstractions;
using TgPoster.Telegram.Models;
using TgPoster.Worker.Domain.Services.ScheduleAlerts;
using TgPoster.Worker.Domain.UseCases.ParseChannel;

namespace TgPoster.Worker.Domain.Tests.ParseChannel;

public sealed class ParseChannelUseCaseShould
{
	private const string ChannelName = "source_channel";

	private readonly Mock<IParseChannelUseCaseStorage> storage = new();
	private readonly Mock<ITelegramMessageService> tgMessages = new();
	private readonly Mock<IScheduleAlertService> alertService = new();
	private readonly Mock<IPublishEndpoint> publishEndpoint = new();
	private readonly Guid id = Guid.NewGuid();
	private readonly Guid scheduleId = Guid.NewGuid();
	private readonly Guid sessionId = Guid.NewGuid();
	private readonly ParseChannelUseCase sut;

	public ParseChannelUseCaseShould()
	{
		SetupSettings(skipAds: false);

		sut = new ParseChannelUseCase(
			storage.Object,
			publishEndpoint.Object,
			tgMessages.Object,
			alertService.Object,
			NullLogger<ParseChannelUseCase>.Instance);
	}

	[Fact]
	public async Task MarkFailedAndNotify_WhenChannelNotFound()
	{
		SetupResolve(TelegramOperationResult<TelegramChatInfo>.Failed(TelegramOperationStatus.UsernameNotFound));

		var result = await sut.Handle(id, CancellationToken.None);

		result.ShouldBeFalse();
		storage.Verify(s => s.UpdateErrorStatusAsync(id, It.IsAny<CancellationToken>()), Times.Once);
		storage.Verify(s => s.UpdateChannelParsingParametersAsync(It.IsAny<Guid>(), It.IsAny<int>(),
			It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
		alertService.Verify(a => a.NotifyAsync(scheduleId, ScheduleProblem.ParsingFailed,
			"Источник @source_channel: канал не найден или недоступен (UsernameNotFound)", id,
			It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task KeepStatusWithoutAlert_WhenFloodWait()
	{
		SetupResolve(TelegramOperationResult<TelegramChatInfo>.Failed(TelegramOperationStatus.FloodWait, floodWait: 60));

		var result = await sut.Handle(id, CancellationToken.None);

		result.ShouldBeTrue();
		storage.Verify(s => s.UpdateErrorStatusAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
		storage.Verify(s => s.UpdateChannelParsingParametersAsync(id, 42, true, It.IsAny<CancellationToken>()),
			Times.Once);
		alertService.Verify(a => a.NotifyAsync(It.IsAny<Guid>(), It.IsAny<ScheduleProblem>(), It.IsAny<string>(),
			It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task MarkFailedAndNotify_WhenHistoryAccessDenied()
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
			.ReturnsAsync(TelegramOperationResult<TelegramHistoryPage>.Failed(TelegramOperationStatus.AccessDenied));

		var result = await sut.Handle(id, CancellationToken.None);

		result.ShouldBeFalse();
		storage.Verify(s => s.UpdateErrorStatusAsync(id, It.IsAny<CancellationToken>()), Times.Once);
		alertService.Verify(a => a.NotifyAsync(scheduleId, ScheduleProblem.ParsingFailed,
			"Источник @source_channel: канал не найден или недоступен (AccessDenied)", id,
			It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task SkipAdAlbum_WhenSkipAdsEnabled()
	{
		SetupSettings(skipAds: true);
		SetupHistoryWithAdAlbumAndRegularPost();

		var result = await sut.Handle(id, CancellationToken.None);

		result.ShouldBeTrue();
		VerifyPublished([52], Times.Once());
		VerifyPublished([50, 51], Times.Never());
	}

	[Fact]
	public async Task PublishAdAlbum_WhenSkipAdsDisabled()
	{
		SetupSettings(skipAds: false);
		SetupHistoryWithAdAlbumAndRegularPost();

		var result = await sut.Handle(id, CancellationToken.None);

		result.ShouldBeTrue();
		VerifyPublished([52], Times.Once());
		VerifyPublished([50, 51], Times.Once());
	}

	private void SetupSettings(bool skipAds)
	{
		storage.Setup(s => s.GetChannelParsingParametersAsync(id, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new ParseChannelParsingSettings
			{
				ChannelName = ChannelName,
				LastParsedId = 42,
				CheckNewPosts = true,
				TelegramSessionId = sessionId,
				ScheduleId = scheduleId,
				SkipAds = skipAds
			});
	}

	private void SetupHistoryWithAdAlbumAndRegularPost()
	{
		SetupResolve(TelegramOperationResult<TelegramChatInfo>.Success(new TelegramChatInfo
		{
			Id = 100,
			AccessHash = 1000,
			Title = "Источник",
			Peer = TelegramPeer.Channel(100, 1000)
		}));

		var date = new DateTime(2026, 10, 24, 12, 0, 0, DateTimeKind.Utc);
		TelegramMessage[] messages =
		[
			new() { Id = 52, Date = date, Text = "Обычный пост" },
			new()
			{
				Id = 51,
				Date = date,
				GroupedId = 777,
				Text = "Скидки на всё!\nРеклама. ООО «Ромашка»",
				Media = new TelegramMessageMedia { Type = TelegramMediaType.Photo }
			},
			new()
			{
				Id = 50,
				Date = date,
				GroupedId = 777,
				Media = new TelegramMessageMedia { Type = TelegramMediaType.Photo }
			},
			new() { Id = 41, Date = date, Text = "Уже спарсенный пост" }
		];
		tgMessages.Setup(t => t.GetHistoryAsync(sessionId, It.IsAny<TelegramPeer>(), It.IsAny<int>(), It.IsAny<int>(),
				It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<CancellationToken>(), It.IsAny<bool>()))
			.ReturnsAsync(TelegramOperationResult<TelegramHistoryPage>.Success(new TelegramHistoryPage
			{
				Messages = messages
			}));
	}

	private void VerifyPublished(int[] messageIds, Times times)
	{
		publishEndpoint.Verify(p => p.Publish(
			It.Is<ProcessMessage>(m => m.Messages.Select(x => x.Id).SequenceEqual(messageIds)),
			It.IsAny<CancellationToken>()), times);
	}

	private void SetupResolve(TelegramOperationResult<TelegramChatInfo> result)
	{
		tgMessages.Setup(t => t.ResolveChannelAsync(sessionId, ChannelName, It.IsAny<CancellationToken>(),
				It.IsAny<bool>()))
			.ReturnsAsync(result);
	}
}
