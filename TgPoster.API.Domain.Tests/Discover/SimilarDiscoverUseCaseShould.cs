using Microsoft.Extensions.Caching.Memory;
using Moq;
using Security.IdentityServices;
using Shouldly;
using TgPoster.API.Domain.UseCases.Discover.SimilarChannels;
using TgPoster.Exceptions.BadRequest;
using TgPoster.Exceptions.NotFound;
using TgPoster.Telegram.Abstractions;
using TgPoster.Telegram.Models;

namespace TgPoster.API.Domain.Tests.Discover;

public sealed class SimilarDiscoverUseCaseShould : IDisposable
{
	private readonly SimilarDiscoverSeed seed = new(Guid.NewGuid(), 10, "source", "channel", null, null, null, null);
	private readonly Guid userId = Guid.NewGuid();
	private readonly Guid sessionId = Guid.NewGuid();
	private readonly Mock<ISimilarDiscoverStorage> storage = new();
	private readonly Mock<ITelegramMessageService> telegram = new();
	private readonly MemoryCache cache = new(new MemoryCacheOptions());
	private readonly SimilarDiscoverUseCase sut;

	public SimilarDiscoverUseCaseShould()
	{
		var identity = new Mock<IIdentityProvider>();
		identity.Setup(x => x.Current).Returns(new Identity(userId));
		storage.Setup(s => s.GetSeedAsync(seed.Id, default)).ReturnsAsync(seed);
		storage.Setup(s => s.GetSessionAsync(userId, default)).ReturnsAsync(sessionId);
		storage.Setup(s => s.FindKnownAsync(It.IsAny<long[]>(), It.IsAny<string[]>(), default)).ReturnsAsync([]);
		storage.Setup(s => s.FindLocalAsync(It.IsAny<SimilarDiscoverSeed>(), default)).ReturnsAsync([]);
		telegram.Setup(s => s.ResolveChannelAsync(sessionId, "source", default, false))
			.ReturnsAsync(TelegramOperationResult<TelegramChatInfo>.Success(Chat(10, "source")));
		sut = new SimilarDiscoverUseCase(storage.Object, identity.Object, telegram.Object, cache);
	}

	public void Dispose() => cache.Dispose();

	[Fact]
	public async Task ShowNewAndKnownPeers_WhenTelegramReturnsDuplicatesAndHiddenChannels()
	{
		var knownId = Guid.NewGuid();
		Recommend(Chat(10, "source"), Chat(20, "one"), Chat(20, "one"), Chat(30, "two"), Chat(40, "three"),
			Chat(50, "four"), Chat(60, "reassigned"));
		storage.Setup(s => s.FindKnownAsync(It.IsAny<long[]>(), It.IsAny<string[]>(), default))
			.ReturnsAsync([new(knownId, null, "TWO", false), new(Guid.NewGuid(), 40, "old_name", true),
				new(Guid.NewGuid(), null, "FOUR", true), new(Guid.NewGuid(), 100, "reassigned", false)]);

		var preview = await sut.Handle(new GetSimilarDiscoverQuery(seed.Id), default);

		preview.Source.ShouldBe(SimilarDiscoverSource.Telegram);
		preview.Channels.Select(c => c.TelegramId).ShouldBe([20L, 30L]);
		preview.Channels[0].DiscoverId.ShouldBeNull();
		preview.Channels[1].DiscoverId.ShouldBe(knownId);
		storage.Verify(s => s.AddAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<IReadOnlyList<SimilarDiscoverChannel>>(),
			default), Times.Never);
	}

	[Fact]
	public async Task SaveOnlySelectedNewChannels_WithoutRequestingTelegramAgain()
	{
		Recommend(Chat(20, "one"), Chat(30, "two"), Chat(40, "three"));
		await sut.Handle(new GetSimilarDiscoverQuery(seed.Id), default);
		storage.Setup(s => s.FindKnownAsync(It.IsAny<long[]>(), It.IsAny<string[]>(), default))
			.ReturnsAsync([new(Guid.NewGuid(), 40, null, false)]);
		storage.Setup(s => s.AddAsync(seed.Id, userId, It.Is<IReadOnlyList<SimilarDiscoverChannel>>(
			c => c.Count == 1 && c[0].TelegramId == 30 && c[0].Username == "two"), default)).ReturnsAsync(1);

		var added = await sut.Handle(new SaveSimilarDiscoverCommand(seed.Id, [30, 40, 30]), default);

		added.ShouldBe(1);
		telegram.Verify(s => s.GetChannelRecommendationsAsync(sessionId, It.IsAny<TelegramPeer>(), default), Times.Once);
	}

	[Fact]
	public async Task RejectWholeSelection_WhenItContainsAnUnknownId()
	{
		Recommend(Chat(20, "one"));

		await Should.ThrowAsync<InvalidDiscoverSelectionException>(() =>
			sut.Handle(new SaveSimilarDiscoverCommand(seed.Id, [20, 999]), default));

		storage.Verify(s => s.AddAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<IReadOnlyList<SimilarDiscoverChannel>>(),
			default), Times.Never);
	}

	[Theory]
	[InlineData("chat", true)]
	[InlineData("channel", false)]
	public async Task ReturnCatalogMatches_WhenTelegramRecommendationsAreUnavailable(string peerType, bool hasSession)
	{
		var source = seed with { PeerType = peerType };
		storage.Setup(s => s.GetSeedAsync(seed.Id, default)).ReturnsAsync(source);
		storage.Setup(s => s.GetSessionAsync(userId, default)).ReturnsAsync(hasSession ? sessionId : null);
		var local = new SimilarDiscoverChannel(Guid.NewGuid(), null, null, "Чат", null, 50);
		storage.Setup(s => s.FindLocalAsync(source, default)).ReturnsAsync([local]);

		var preview = await sut.Handle(new GetSimilarDiscoverQuery(seed.Id), default);

		preview.Source.ShouldBe(SimilarDiscoverSource.Catalog);
		preview.Channels.ShouldBe([local]);
		telegram.VerifyNoOtherCalls();
	}

	[Fact]
	public async Task UseCatalog_WhenSourceUsernameNowBelongsToAnotherChannel()
	{
		telegram.Setup(t => t.ResolveChannelAsync(sessionId, "source", default, false))
			.ReturnsAsync(TelegramOperationResult<TelegramChatInfo>.Success(Chat(99, "source")));

		var preview = await sut.Handle(new GetSimilarDiscoverQuery(seed.Id), default);

		preview.Source.ShouldBe(SimilarDiscoverSource.Catalog);
		telegram.Verify(t => t.GetChannelRecommendationsAsync(It.IsAny<Guid>(), It.IsAny<TelegramPeer>(), default), Times.Never);
	}

	[Fact]
	public async Task ReportNotFound_WhenSourceIsMissing()
	{
		await Should.ThrowAsync<DiscoveredChannelNotFoundException>(() =>
			sut.Handle(new GetSimilarDiscoverQuery(Guid.NewGuid()), default));

		telegram.VerifyNoOtherCalls();
	}

	private void Recommend(params TelegramChatInfo[] channels) =>
		telegram.Setup(t => t.GetChannelRecommendationsAsync(sessionId, TelegramPeer.Channel(10, 1), default))
			.ReturnsAsync(TelegramOperationResult<IReadOnlyList<TelegramChatInfo>>.Success(channels));

	private static TelegramChatInfo Chat(long id, string username) => new()
	{
		Id = id, Username = username, Title = "Название", AccessHash = 1, IsChannel = true, Peer = TelegramPeer.Channel(id, 1)
	};
}
