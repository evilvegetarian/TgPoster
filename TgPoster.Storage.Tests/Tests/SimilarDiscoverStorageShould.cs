using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using Shouldly;
using TgPoster.API.Domain.UseCases.Discover.SimilarChannels;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.Enum;
using TgPoster.Storage.Storages;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public sealed class SimilarDiscoverStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context = fixture.GetDbContext();
	private readonly SimilarDiscoverStorage sut = new(fixture.GetDbContext(), new GuidFactory());

	[Fact]
	public async Task RankLocalPeersByTopicTagsAndLanguage_AndExcludeHiddenPeers()
	{
		var topic = Guid.NewGuid().ToString("N");
		var seed = new DiscoveredChannel { Id = Guid.NewGuid(), Subcategory = topic, Tags = [" Game "], Language = "ru" };
		var exact = new DiscoveredChannel { Id = Guid.NewGuid(), Subcategory = topic, Tags = ["GAME"], Language = "ru" };
		var otherLanguage = new DiscoveredChannel { Id = Guid.NewGuid(), Subcategory = topic, Tags = ["game"], Language = "en" };
		var tagOnly = new DiscoveredChannel { Id = Guid.NewGuid(), Tags = [" game "] };
		var languageOnly = new DiscoveredChannel { Id = Guid.NewGuid(), Language = "ru" };
		var banned = new DiscoveredChannel { Id = Guid.NewGuid(), Subcategory = topic, IsBanned = true };
		context.DiscoveredChannels.AddRange(seed, exact, otherLanguage, tagOnly, languageOnly, banned);
		await context.SaveChangesAsync();

		var source = await sut.GetSeedAsync(seed.Id, default);
		var matches = await sut.FindLocalAsync(source.ShouldNotBeNull(), default);

		matches.Select(c => c.DiscoverId).ShouldBe([exact.Id, otherLanguage.Id, tagOnly.Id]);
		(await sut.GetSeedAsync(banned.Id, default)).ShouldBeNull();
	}

	[Fact]
	public async Task UseOnlyAvailableDiscoverSessionOwnedByCurrentUser()
	{
		var user = await new UserBuilder(context).CreateAsync();
		await new TelegramSessionBuilder(context).WithStatus(TelegramSessionStatus.Authorized)
			.WithPurposes(TelegramSessionPurpose.Discover).CreateAsync();
		await new TelegramSessionBuilder(context).WithUserId(user.Id).WithStatus(TelegramSessionStatus.Authorized).CreateAsync();
		var cooldown = await new TelegramSessionBuilder(context).WithUserId(user.Id).WithStatus(TelegramSessionStatus.Authorized)
			.WithPurposes(TelegramSessionPurpose.Discover).CreateAsync();
		cooldown.FloodWaitUntil = DateTimeOffset.UtcNow.AddHours(1);
		await context.SaveChangesAsync();

		(await sut.GetSessionAsync(user.Id, default)).ShouldBeNull();

		var eligible = await new TelegramSessionBuilder(context).WithUserId(user.Id).WithStatus(TelegramSessionStatus.Authorized)
			.WithPurposes(TelegramSessionPurpose.Discover).CreateAsync();
		(await sut.GetSessionAsync(user.Id, default)).ShouldBe(eligible.Id);
	}

	[Fact]
	public async Task InsertSelectedPeersOnce_AndPreserveExistingAndHiddenCatalogEntries()
	{
		var marker = Guid.NewGuid().ToString("N");
		var source = new DiscoveredChannel { Id = Guid.NewGuid() };
		var known = new DiscoveredChannel { Id = Guid.NewGuid(), TelegramId = 901, Title = "Старое название" };
		var hidden = new DiscoveredChannel { Id = Guid.NewGuid(), Username = "Hidden" + marker, IsBanned = true };
		context.DiscoveredChannels.AddRange(source, known, hidden);
		await context.SaveChangesAsync();
		var user = await new UserBuilder(context).CreateAsync();
		SimilarDiscoverChannel[] selected = [
			new(null, 901, "existing" + marker, "Новое название", null, null),
			new(null, 902, "hidden" + marker, "Скрытый", null, 20),
			new(null, 903, "new" + marker, "Новый", null, null)
		];

		await using var otherContext = fixture.GetDbContext();
		var other = new SimilarDiscoverStorage(otherContext, new GuidFactory());
		var counts = await Task.WhenAll(sut.AddAsync(source.Id, user.Id, selected, default),
			other.AddAsync(source.Id, user.Id, selected, default));

		counts.Sum().ShouldBe(1);
		var added = await context.DiscoveredChannels.SingleAsync(c => c.DiscoveredFromChannelId == source.Id);
		added.TelegramId.ShouldBe(903);
		added.CreatedById.ShouldBe(user.Id);
		added.TgUrl.ShouldBe("https://t.me/new" + marker);
		added.Status.ShouldBe(DiscoveryStatus.Pending);
		(await context.DiscoveredChannels.AsNoTracking().FirstAsync(c => c.Id == known.Id)).Title.ShouldBe("Старое название");
		var matches = await sut.FindKnownAsync([901], ["HIDDEN" + marker], default);
		matches.ShouldContain(p => p.Id == hidden.Id && p.Hidden);
		matches.ShouldContain(p => p.Id == known.Id && !p.Hidden);
	}
}
