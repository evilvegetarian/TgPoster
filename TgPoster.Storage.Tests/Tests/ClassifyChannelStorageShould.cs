using Microsoft.EntityFrameworkCore;
using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.Enum;
using TgPoster.Storage.Storages.ClassifyChannel;
using TgPoster.Storage.Tests.Builders;
using TgPoster.Worker.Domain.UseCases.ClassifyChannel;

namespace TgPoster.Storage.Tests.Tests;

public sealed class ClassifyChannelStorageShould(StorageTestFixture fixture)
	: IClassFixture<StorageTestFixture>
{
	private static readonly DateTimeOffset Now = new(2026, 9, 26, 12, 0, 0, TimeSpan.Zero);

	private readonly PosterContext context = fixture.GetDbContext();
	private readonly ClassifyChannelStorage sut = new(fixture.GetDbContext());

	[Fact]
	public async Task EnsureSettingsAsync_ShouldCreateOnce_AndKeepExistingValues()
	{
		await sut.EnsureSettingsAsync(Settings() with { Model = "first/model" }, CancellationToken.None);
		await sut.EnsureSettingsAsync(Settings() with { Model = "second/model" }, CancellationToken.None);

		var result = await sut.GetSettingsAsync(CancellationToken.None);

		result.ShouldNotBeNull();
		result.Model.ShouldBe("first/model");
		result.Categories.ShouldBe(["Технологии", "Другое"]);
		(await context.ClassifierSettings.CountAsync(CancellationToken.None)).ShouldBe(1);
	}

	[Fact]
	public async Task GetSettingsAsync_ShouldDropInactiveSession()
	{
		await sut.EnsureSettingsAsync(Settings(), CancellationToken.None);
		var session = new TelegramSessionBuilder(context).WithIsActive(false).Create();
		var entity = await context.ClassifierSettings.FirstAsync(CancellationToken.None);
		entity.TelegramSessionId = session.Id;
		await context.SaveChangesAsync(CancellationToken.None);

		var inactive = await sut.GetSettingsAsync(CancellationToken.None);
		session.IsActive = true;
		context.TelegramSessions.Update(session);
		await context.SaveChangesAsync(CancellationToken.None);
		var active = await sut.GetSettingsAsync(CancellationToken.None);

		inactive.ShouldNotBeNull();
		inactive.TelegramSessionId.ShouldBeNull();
		active.ShouldNotBeNull();
		active.TelegramSessionId.ShouldBe(session.Id);
	}

	[Fact]
	public async Task GetChannelsToClassifyAsync_ShouldPreferNeverClassified_ThenNeverAttempted_ThenOldestAttempt()
	{
		await ClearChannelsAsync();
		var oldAttempt = NewChannel(c => c.LastClassificationAttemptAt = Now.AddDays(-3));
		var newerAttempt = NewChannel(c => c.LastClassificationAttemptAt = Now.AddDays(-1));
		var neverAttempted = NewChannel();
		var classified = NewChannel(c =>
		{
			c.LastClassifiedAt = Now.AddDays(-100);
			c.LastClassificationAttemptAt = Now.AddDays(-100);
		});
		context.DiscoveredChannels.AddRange(oldAttempt, newerAttempt, neverAttempted, classified);
		await context.SaveChangesAsync(CancellationToken.None);

		var result = await sut.GetChannelsToClassifyAsync(10, Now, Now.AddDays(-30), CancellationToken.None);

		result.Select(x => x.Id).ShouldBe([neverAttempted.Id, oldAttempt.Id, newerAttempt.Id, classified.Id]);
	}

	[Fact]
	public async Task GetChannelsToClassifyAsync_ShouldPostponeRecentAttempts()
	{
		await ClearChannelsAsync();
		var recentlyFailed = NewChannel(c => c.LastClassificationAttemptAt = Now.AddHours(-1));
		var failedLongAgo = NewChannel(c => c.LastClassificationAttemptAt = Now.AddHours(-10));
		context.DiscoveredChannels.AddRange(recentlyFailed, failedLongAgo);
		await context.SaveChangesAsync(CancellationToken.None);

		var result = await sut.GetChannelsToClassifyAsync(10, Now.AddHours(-6), null, CancellationToken.None);

		result.Select(x => x.Id).ShouldBe([failedLongAgo.Id]);
	}

	[Fact]
	public async Task GetChannelsToClassifyAsync_ShouldReclassifyOnlyStaleChannels_AndNeverWithoutWindow()
	{
		await ClearChannelsAsync();
		var stale = NewChannel(c =>
		{
			c.LastClassifiedAt = Now.AddDays(-40);
			c.LastClassificationAttemptAt = Now.AddDays(-40);
		});
		var fresh = NewChannel(c =>
		{
			c.LastClassifiedAt = Now.AddDays(-5);
			c.LastClassificationAttemptAt = Now.AddDays(-5);
		});
		context.DiscoveredChannels.AddRange(stale, fresh);
		await context.SaveChangesAsync(CancellationToken.None);

		var withWindow = await sut.GetChannelsToClassifyAsync(10, Now, Now.AddDays(-30), CancellationToken.None);
		var withoutWindow = await sut.GetChannelsToClassifyAsync(10, Now, null, CancellationToken.None);

		withWindow.Select(x => x.Id).ShouldBe([stale.Id]);
		withoutWindow.ShouldBeEmpty();
	}

	[Fact]
	public async Task GetChannelsToClassifyAsync_ShouldSkipPrivateChannels_AndRespectBatchSize()
	{
		await ClearChannelsAsync();
		context.DiscoveredChannels.AddRange(
			NewChannel(c => c.Username = null),
			NewChannel(),
			NewChannel(),
			NewChannel());
		await context.SaveChangesAsync(CancellationToken.None);

		var result = await sut.GetChannelsToClassifyAsync(2, Now, null, CancellationToken.None);

		result.Count.ShouldBe(2);
		result.ShouldAllBe(x => x.Username != null);
	}

	[Fact]
	public async Task MarkClassificationAttemptAsync_ShouldStoreAttemptTime()
	{
		var channel = NewChannel();
		context.DiscoveredChannels.Add(channel);
		await context.SaveChangesAsync(CancellationToken.None);

		await sut.MarkClassificationAttemptAsync(channel.Id, Now, CancellationToken.None);

		using var check = fixture.GetDbContext();
		var saved = await check.DiscoveredChannels.FirstAsync(x => x.Id == channel.Id, CancellationToken.None);
		saved.LastClassificationAttemptAt.ShouldBe(Now);
		saved.LastClassifiedAt.ShouldBeNull();
	}

	// Удаление здесь мягкое, а фильтр запросов у каналов смотрит только на IsBanned —
	// поэтому прячем каналы прошлых тестов баном
	private async Task ClearChannelsAsync()
	{
		var all = await context.DiscoveredChannels.ToListAsync(CancellationToken.None);
		all.ForEach(x => x.IsBanned = true);
		await context.SaveChangesAsync(CancellationToken.None);
		context.ChangeTracker.Clear();
	}

	private static ClassifierSettingsDto Settings() => new()
	{
		IsEnabled = true,
		Model = "some/model",
		BatchSize = 2,
		IntervalMinutes = 20,
		MessageSampleCount = 25,
		PhotoCount = 0,
		Categories = ["Технологии", "Другое"],
		SystemPrompt = "Промпт {categories}"
	};

	private static DiscoveredChannel NewChannel(Action<DiscoveredChannel>? setup = null)
	{
		var channel = new DiscoveredChannel
		{
			Id = Guid.NewGuid(),
			Username = $"q_{Guid.NewGuid():N}",
			Title = "Channel",
			PeerType = "channel",
			Status = DiscoveryStatus.Pending
		};
		setup?.Invoke(channel);
		return channel;
	}
}
