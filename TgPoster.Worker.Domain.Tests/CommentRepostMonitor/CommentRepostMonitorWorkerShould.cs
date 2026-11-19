using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using TgPoster.Telegram.Abstractions;
using TgPoster.Telegram.Models;
using TgPoster.Worker.Domain.UseCases.CommentRepostMonitor;
using TgPoster.Worker.Domain.UseCases.SendCommentConsumer;

namespace TgPoster.Worker.Domain.Tests.CommentRepostMonitor;

public sealed class CommentRepostMonitorWorkerShould
{
	private readonly Mock<ICommentRepostMonitorStorage> storage = new();
	private readonly Mock<ITelegramMessageService> messages = new();
	private readonly Mock<IPublishEndpoint> publisher = new();
	private readonly CommentRepostSettingDto setting = new()
	{
		Id = Guid.NewGuid(), TelegramSessionId = Guid.NewGuid(), WatchedChannelId = 100,
		WatchedChannelAccessHash = 200, DiscussionGroupId = 300, DiscussionGroupAccessHash = 400,
		SourceChannelId = 500, LastProcessedPostId = 10
	};
	private readonly CommentRepostMonitorWorker sut;

	public CommentRepostMonitorWorkerShould()
	{
		storage.Setup(x => x.GetActiveSettingsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([setting]);
		sut = new(storage.Object, messages.Object, publisher.Object,
			NullLogger<CommentRepostMonitorWorker>.Instance, Mock.Of<IHostApplicationLifetime>());
	}

	[Fact]
	public async Task SkipTelegram_WhenNoSettingsAreActive()
	{
		storage.Setup(x => x.GetActiveSettingsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

		await sut.CheckForNewPostsAsync();

		messages.VerifyNoOtherCalls();
		publisher.VerifyNoOtherCalls();
	}

	[Fact]
	public async Task PublishNewPostsInOrder_AndAdvanceCursorOnlyAfterPublishing()
	{
		SetupHistory(12, 9, 11, 10);
		var published = new List<SendCommentCommand>();
		publisher.Setup(x => x.Publish(It.IsAny<SendCommentCommand>(), It.IsAny<CancellationToken>()))
			.Callback<SendCommentCommand, CancellationToken>((command, _) => published.Add(command))
			.Returns(Task.CompletedTask);
		storage.Setup(x => x.UpdateLastProcessedAsync(setting.Id, 12, It.IsAny<CancellationToken>()))
			.Callback(() => published.Select(x => x.OriginalPostId).ShouldBe([11, 12]));

		await sut.CheckForNewPostsAsync();

		published.Count.ShouldBe(2);
		published[0].CommentRepostSettingsId.ShouldBe(setting.Id);
		published[0].TelegramSessionId.ShouldBe(setting.TelegramSessionId);
		published[0].WatchedChannelId.ShouldBe(100);
		published[0].WatchedChannelAccessHash.ShouldBe(200);
		published[0].DiscussionGroupId.ShouldBe(300);
		published[0].DiscussionGroupAccessHash.ShouldBe(400);
		published[0].SourceChannelId.ShouldBe(500);
		storage.Verify(x => x.UpdateLastProcessedAsync(setting.Id, 12, It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task KeepCursor_WhenThereAreNoNewPosts()
	{
		SetupHistory(10, 9);

		await sut.CheckForNewPostsAsync();

		publisher.VerifyNoOtherCalls();
		storage.Verify(x => x.UpdateLastProcessedAsync(It.IsAny<Guid>(), It.IsAny<int>(),
			It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task KeepCursor_WhenTelegramFails()
	{
		SetupResult(TelegramOperationResult<TelegramHistoryPage>.Failed(TelegramOperationStatus.Timeout));

		await sut.CheckForNewPostsAsync();

		publisher.VerifyNoOtherCalls();
		storage.Verify(x => x.UpdateLastProcessedAsync(It.IsAny<Guid>(), It.IsAny<int>(),
			It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task KeepCursor_AndContinueWithNextSetting_WhenPublishingFails()
	{
		var next = setting with { Id = Guid.NewGuid() };
		storage.Setup(x => x.GetActiveSettingsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([setting, next]);
		SetupHistory(11);
		publisher.Setup(x => x.Publish(It.Is<SendCommentCommand>(c => c.CommentRepostSettingsId == setting.Id),
			It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("publish failed"));

		await sut.CheckForNewPostsAsync();

		storage.Verify(x => x.UpdateLastProcessedAsync(setting.Id, It.IsAny<int>(),
			It.IsAny<CancellationToken>()), Times.Never);
		storage.Verify(x => x.UpdateLastProcessedAsync(next.Id, 11, It.IsAny<CancellationToken>()), Times.Once);
	}

	private void SetupHistory(params int[] ids) => SetupResult(
		TelegramOperationResult<TelegramHistoryPage>.Success(new TelegramHistoryPage
		{
			Messages = ids.Select(id => new TelegramMessage { Id = id, Date = DateTime.UtcNow }).ToList()
		}));

	private void SetupResult(TelegramOperationResult<TelegramHistoryPage> result) =>
		messages.Setup(x => x.GetHistoryAsync(setting.TelegramSessionId, It.IsAny<TelegramPeer>(),
			20, 0, null, 10, It.IsAny<CancellationToken>(), true)).ReturnsAsync(result);
}
