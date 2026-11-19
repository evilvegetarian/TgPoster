using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shared.Enums;
using Shouldly;
using TgPoster.Telegram.Abstractions;
using TgPoster.Telegram.Models;
using TgPoster.Worker.Domain.UseCases.UpdateChannelStats;

namespace TgPoster.Worker.Domain.Tests.UpdateChannelStats;

public sealed class UpdateChannelStatsWorkerShould
{
	private readonly Guid sessionId = Guid.NewGuid();
	private readonly ChannelStatsDto channel = new(Guid.NewGuid(), "channel", 123);
	private readonly Mock<IUpdateChannelStatsStorage> storage = new();
	private readonly Mock<ITelegramMessageService> messages = new();
	private readonly UpdateChannelStatsWorker sut;

	public UpdateChannelStatsWorkerShould()
	{
		storage.Setup(x => x.GetSessionIdByPurposeAsync(TelegramSessionPurpose.UpdateStats,
			It.IsAny<CancellationToken>())).ReturnsAsync(sessionId);
		storage.Setup(x => x.GetChannelsToUpdateAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync([channel]);
		messages.Setup(x => x.ResolveChannelAsync(sessionId, channel.Username, It.IsAny<CancellationToken>(), false))
			.ReturnsAsync(TelegramOperationResult<TelegramChatInfo>.Success(new TelegramChatInfo
			{
				Id = 123, AccessHash = 456, Title = "Channel", Peer = TelegramPeer.Channel(123, 456)
			}));
		sut = new(storage.Object, messages.Object, NullLogger<UpdateChannelStatsWorker>.Instance,
			Mock.Of<IHostApplicationLifetime>());
	}

	[Fact]
	public async Task NotQueryChannels_WhenNoStatsSessionExists()
	{
		storage.Setup(x => x.GetSessionIdByPurposeAsync(TelegramSessionPurpose.UpdateStats,
			It.IsAny<CancellationToken>())).ReturnsAsync((Guid?)null);

		await sut.UpdateStatsAsync();

		storage.Verify(x => x.GetChannelsToUpdateAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
		messages.VerifyNoOtherCalls();
	}

	[Fact]
	public async Task SkipTelegram_WhenNoChannelsNeedUpdating()
	{
		storage.Setup(x => x.GetChannelsToUpdateAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync([]);

		await sut.UpdateStatsAsync();

		messages.VerifyNoOtherCalls();
	}

	[Theory]
	[InlineData(0)]
	[InlineData(100)]
	public async Task SaveParticipantCount_WhenTelegramReturnsIt(int count)
	{
		messages.Setup(x => x.GetFullChannelAsync(sessionId, It.IsAny<TelegramPeer>(),
			It.IsAny<CancellationToken>(), false)).ReturnsAsync(TelegramOperationResult<int?>.Success(count));

		await sut.UpdateStatsAsync();

		storage.Verify(x => x.UpdateParticipantsCountAsync(channel.Id, count,
			It.IsAny<CancellationToken>()), Times.Once);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task KeepStoredCount_WhenStatsAreUnavailable(bool successfulResponse)
	{
		var result = successfulResponse ? TelegramOperationResult<int?>.Success(null)
			: TelegramOperationResult<int?>.Failed(TelegramOperationStatus.AccessDenied);
		messages.Setup(x => x.GetFullChannelAsync(sessionId, It.IsAny<TelegramPeer>(),
			It.IsAny<CancellationToken>(), false)).ReturnsAsync(result);

		await sut.UpdateStatsAsync();

		storage.Verify(x => x.UpdateParticipantsCountAsync(It.IsAny<Guid>(), It.IsAny<int>(),
			It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task SkipFullChannelLookup_WhenResolvingChannelFails()
	{
		messages.Setup(x => x.ResolveChannelAsync(sessionId, channel.Username, It.IsAny<CancellationToken>(), false))
			.ReturnsAsync(TelegramOperationResult<TelegramChatInfo>.Failed(TelegramOperationStatus.UsernameNotFound));

		await sut.UpdateStatsAsync();

		messages.Verify(x => x.GetFullChannelAsync(It.IsAny<Guid>(), It.IsAny<TelegramPeer>(),
			It.IsAny<CancellationToken>(), It.IsAny<bool>()), Times.Never);
	}
}
