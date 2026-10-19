using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.Enum;
using TgPoster.Storage.Storages.Repost;
using TgPoster.Storage.Tests.Builders;
using TgPoster.Worker.Domain.UseCases.RepostMessageConsumer;

namespace TgPoster.Storage.Tests.Tests;

public sealed class RepostMessageConsumerStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context = fixture.GetDbContext();
	private readonly RepostMessageConsumerStorage sut = new(fixture.GetDbContext(), new GuidFactory());

	[Fact]
	public async Task GetRepostDataAsync_WithActiveSettings_ShouldReturnRepostData()
	{
		var session = await new TelegramSessionBuilder(context).CreateAsync();
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var msg = new Message
		{
			Id = Guid.NewGuid(),
			ScheduleId = schedule.Id,
			TimePosting = DateTimeOffset.UtcNow.AddMinutes(1),
			Status = MessageStatus.Send,
			TextMessage = "test",
			IsTextMessage = false,
			TelegramMessageId = 12345
		};
		await context.Messages.AddAsync(msg);
		await context.SaveChangesAsync();
		var channel1 = 1241241245;
		var channel2 = 46489315;
		var settings = await new RepostSettingsBuilder(context)
			.WithScheduleId(schedule.Id)
			.WithTelegramSessionId(session.Id)
			.WithIsActive(true)
			.CreateAsync();

		await new RepostDestinationBuilder(context)
			.WithRepostSettingsId(settings.Id)
			.WithChatIdentifier(channel1)
			.WithIsActive(true)
			.CreateAsync();

		await new RepostDestinationBuilder(context)
			.WithRepostSettingsId(settings.Id)
			.WithChatIdentifier(channel2)
			.WithIsActive(true)
			.CreateAsync();

		var result = await sut.GetRepostDataAsync(msg.Id, settings.Id, CancellationToken.None);

		result.ShouldNotBeNull();
		result.TelegramMessageId.ShouldBe(12345);
		result.TelegramSessionId.ShouldBe(session.Id);
		result.SourceChannelIdentifier.ShouldBe(schedule.ChannelName);
		result.Destinations.Count.ShouldBe(2);
		result.Destinations.ShouldContain(d => d.ChatIdentifier == channel1);
		result.Destinations.ShouldContain(d => d.ChatIdentifier == channel2);
	}

	[Fact]
	public async Task GetRepostDataAsync_WithInactiveSettings_ShouldReturnNull()
	{
		var session = await new TelegramSessionBuilder(context).CreateAsync();
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var msg = new Message
		{
			Id = Guid.NewGuid(),
			ScheduleId = schedule.Id,
			TimePosting = DateTimeOffset.UtcNow.AddMinutes(1),
			Status = MessageStatus.Send,
			TextMessage = "test",
			IsTextMessage = false,
			TelegramMessageId = 12345
		};
		await context.Messages.AddAsync(msg);
		await context.SaveChangesAsync();

		var settings = await new RepostSettingsBuilder(context)
			.WithScheduleId(schedule.Id)
			.WithTelegramSessionId(session.Id)
			.WithIsActive(false)
			.CreateAsync();

		var result = await sut.GetRepostDataAsync(msg.Id, settings.Id, CancellationToken.None);

		result.ShouldBeNull();
	}

	[Fact]
	public async Task GetRepostDataAsync_WithInactiveDestinations_ShouldReturnOnlyActive()
	{
		var session = await new TelegramSessionBuilder(context).CreateAsync();
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var msg = new Message
		{
			Id = Guid.NewGuid(),
			ScheduleId = schedule.Id,
			TimePosting = DateTimeOffset.UtcNow.AddMinutes(1),
			Status = MessageStatus.Send,
			TextMessage = "test",
			IsTextMessage = false,
			TelegramMessageId = 12345
		};
		await context.Messages.AddAsync(msg);
		await context.SaveChangesAsync();
		var active = 1412342421;
		var inactive = 1412342421241;
		var settings = await new RepostSettingsBuilder(context)
			.WithScheduleId(schedule.Id)
			.WithTelegramSessionId(session.Id)
			.WithIsActive(true)
			.CreateAsync();

		await new RepostDestinationBuilder(context)
			.WithRepostSettingsId(settings.Id)
			.WithChatIdentifier(active)
			.WithIsActive(true)
			.CreateAsync();

		await new RepostDestinationBuilder(context)
			.WithRepostSettingsId(settings.Id)
			.WithChatIdentifier(inactive)
			.WithIsActive(false)
			.CreateAsync();

		var result = await sut.GetRepostDataAsync(msg.Id, settings.Id, CancellationToken.None);

		result.ShouldNotBeNull();
		result.Destinations.Count.ShouldBe(1);
		result.Destinations[0].ChatIdentifier.ShouldBe(active);
	}

	[Fact]
	public async Task GetRepostDataAsync_WithNoRepostSettings_ShouldReturnNull()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var msg = new Message
		{
			Id = Guid.NewGuid(),
			ScheduleId = schedule.Id,
			TimePosting = DateTimeOffset.UtcNow.AddMinutes(1),
			Status = MessageStatus.Send,
			TextMessage = "test",
			IsTextMessage = false,
			TelegramMessageId = 12345
		};
		await context.Messages.AddAsync(msg);
		await context.SaveChangesAsync();

		var result = await sut.GetRepostDataAsync(msg.Id, Guid.NewGuid(), CancellationToken.None);

		result.ShouldBeNull();
	}

	[Fact]
	public async Task GetRepostDataAsync_WithNonExistingMessage_ShouldReturnNull()
	{
		var nonExistingId = Guid.NewGuid();

		var result = await sut.GetRepostDataAsync(nonExistingId, nonExistingId, CancellationToken.None);

		result.ShouldBeNull();
	}

	[Fact]
	public async Task CreateRepostLogsAsync_WithSuccess_ShouldCreateLogWithSuccessStatus()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var msg = new Message
		{
			Id = Guid.NewGuid(),
			ScheduleId = schedule.Id,
			TimePosting = DateTimeOffset.UtcNow.AddMinutes(1),
			Status = MessageStatus.Send,
			TextMessage = "test",
			IsTextMessage = false
		};
		await context.Messages.AddAsync(msg);
		await context.SaveChangesAsync();

		var session = await new TelegramSessionBuilder(context).CreateAsync();
		var settings = await new RepostSettingsBuilder(context)
			.WithScheduleId(schedule.Id)
			.WithTelegramSessionId(session.Id)
			.CreateAsync();

		var destination = await new RepostDestinationBuilder(context)
			.WithRepostSettingsId(settings.Id)
			.CreateAsync();

		var telegramMessageId = 54321;

		await sut.CreateRepostLogsAsync([
			new RepostLogEntry
			{
				MessageId = msg.Id,
				RepostDestinationId = destination.Id,
				Status = RepostStatus.Success,
				Reason = RepostLogReason.None,
				TelegramMessageId = telegramMessageId
			}
		], CancellationToken.None);

		var log = await context.Set<RepostLog>()
			.FirstOrDefaultAsync(l => l.MessageId == msg.Id && l.RepostDestinationId == destination.Id);

		log.ShouldNotBeNull();
		log.TelegramMessageId.ShouldBe(telegramMessageId);
		log.Status.ShouldBe(RepostStatus.Success);
		log.Reason.ShouldBe(RepostLogReason.None);
		log.RepostedAt.ShouldNotBeNull();
		log.Error.ShouldBeNull();
	}

	[Fact]
	public async Task CreateRepostLogsAsync_WithError_ShouldCreateLogWithFailedStatus()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var msg = new Message
		{
			Id = Guid.NewGuid(),
			ScheduleId = schedule.Id,
			TimePosting = DateTimeOffset.UtcNow.AddMinutes(1),
			Status = MessageStatus.Send,
			TextMessage = "test",
			IsTextMessage = false
		};
		await context.Messages.AddAsync(msg);
		await context.SaveChangesAsync();

		var session = await new TelegramSessionBuilder(context).CreateAsync();
		var settings = await new RepostSettingsBuilder(context)
			.WithScheduleId(schedule.Id)
			.WithTelegramSessionId(session.Id)
			.CreateAsync();

		var destination = await new RepostDestinationBuilder(context)
			.WithRepostSettingsId(settings.Id)
			.CreateAsync();

		var errorMessage = "Failed to repost";

		await sut.CreateRepostLogsAsync([
			new RepostLogEntry
			{
				MessageId = msg.Id,
				RepostDestinationId = destination.Id,
				Status = RepostStatus.Failed,
				Reason = RepostLogReason.ForwardFailed,
				Error = errorMessage
			}
		], CancellationToken.None);

		var log = await context.Set<RepostLog>()
			.FirstOrDefaultAsync(l => l.MessageId == msg.Id && l.RepostDestinationId == destination.Id);

		log.ShouldNotBeNull();
		log.TelegramMessageId.ShouldBeNull();
		log.Status.ShouldBe(RepostStatus.Failed);
		log.Reason.ShouldBe(RepostLogReason.ForwardFailed);
		log.RepostedAt.ShouldBeNull();
		log.Error.ShouldBe(errorMessage);
	}

	[Fact]
	public async Task GetRepostDataAsync_WithRecordedAccountBan_ShouldMarkOnlyBannedDestination()
	{
		var session = await new TelegramSessionBuilder(context).CreateAsync();
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var msg = await new MessageBuilder(context).WithScheduleId(schedule.Id).CreateAsync();
		var settings = await new RepostSettingsBuilder(context)
			.WithScheduleId(schedule.Id)
			.WithTelegramSessionId(session.Id)
			.WithIsActive(true)
			.CreateAsync();
		var banned = await new RepostDestinationBuilder(context).WithRepostSettingsId(settings.Id).CreateAsync();
		var available = await new RepostDestinationBuilder(context).WithRepostSettingsId(settings.Id).CreateAsync();
		await new TelegramSessionChatBanBuilder(context)
			.WithTelegramSessionId(session.Id)
			.WithChatId(banned.ChatId)
			.CreateAsync();
		// Бан другого аккаунта в этом чате к сессии настроек не относится
		await new TelegramSessionChatBanBuilder(context)
			.WithChatId(available.ChatId)
			.CreateAsync();

		var result = await sut.GetRepostDataAsync(msg.Id, settings.Id, CancellationToken.None);

		result.ShouldNotBeNull();
		result.Destinations.Single(x => x.Id == banned.Id).IsAccountBanned.ShouldBeTrue();
		result.Destinations.Single(x => x.Id == available.Id).IsAccountBanned.ShouldBeFalse();
	}

	[Fact]
	public async Task MarkAccountBannedAsync_ShouldRecordBanAndDisableDestinationsOfSessionInAllSettings()
	{
		var session = await new TelegramSessionBuilder(context).CreateAsync();
		var chatId = Random.Shared.NextInt64(1_000_000, long.MaxValue);
		var first = await CreateDestinationAsync(session.Id, chatId);
		var second = await CreateDestinationAsync(session.Id, chatId);
		var otherChat = await CreateDestinationAsync(session.Id, chatId - 1);
		var otherSession = await CreateDestinationAsync((await new TelegramSessionBuilder(context).CreateAsync()).Id,
			chatId);

		await sut.MarkAccountBannedAsync(session.Id, chatId, "CHANNEL_PRIVATE", CancellationToken.None);

		var ban = await context.TelegramSessionChatBans
			.AsNoTracking()
			.SingleAsync(x => x.TelegramSessionId == session.Id);
		ban.ChatId.ShouldBe(chatId);
		ban.Error.ShouldBe("CHANNEL_PRIVATE");
		foreach (var id in new[] { first.Id, second.Id })
		{
			var destination = await LoadDestinationAsync(id);
			destination.IsActive.ShouldBeFalse();
			// Бан хранится только в реестре, статус членства остаётся прежним
			destination.ChatStatus.ShouldBe(ChatStatus.Active);
		}

		(await LoadDestinationAsync(otherChat.Id)).IsActive.ShouldBeTrue();
		(await LoadDestinationAsync(otherSession.Id)).IsActive.ShouldBeTrue();
	}

	[Fact]
	public async Task DeactivateDestinationAsync_ShouldDisableAndKeepMembership()
	{
		var session = await new TelegramSessionBuilder(context).CreateAsync();
		var destination = await CreateDestinationAsync(session.Id, Random.Shared.NextInt64(1_000_000, long.MaxValue));

		await sut.DeactivateDestinationAsync(destination.Id, CancellationToken.None);

		var updated = await LoadDestinationAsync(destination.Id);
		updated.IsActive.ShouldBeFalse();
		updated.ChatStatus.ShouldBe(ChatStatus.Active);
	}

	[Fact]
	public async Task MarkAccountBannedAsync_Twice_ShouldKeepSingleBan()
	{
		var session = await new TelegramSessionBuilder(context).CreateAsync();

		await sut.MarkAccountBannedAsync(session.Id, 555, "CHANNEL_PRIVATE", CancellationToken.None);
		await sut.MarkAccountBannedAsync(session.Id, 555, "CHAT_RESTRICTED", CancellationToken.None);

		var ban = await context.TelegramSessionChatBans
			.AsNoTracking()
			.SingleAsync(x => x.TelegramSessionId == session.Id);
		ban.Error.ShouldBe("CHAT_RESTRICTED");
	}

	[Fact]
	public async Task DisableForChatRestrictionAsync_ShouldDisableDestinationAndRecordRestrictionInDiscover()
	{
		var session = await new TelegramSessionBuilder(context).CreateAsync();
		var chatId = Random.Shared.NextInt64(1_000_000, long.MaxValue);
		var discovered = await CreateDiscoveredChannelAsync(chatId);
		var destination = await CreateDestinationAsync(session.Id, chatId, discovered.Id);

		await sut.DisableForChatRestrictionAsync(destination.Id, null, false, CancellationToken.None);

		var updated = await LoadDestinationAsync(destination.Id);
		updated.IsActive.ShouldBeFalse();
		updated.ChatStatus.ShouldBe(ChatStatus.Active);
		var channel = await context.DiscoveredChannels.AsNoTracking().SingleAsync(x => x.Id == discovered.Id);
		channel.CanSendMessages.ShouldBe(true);
		channel.CanSendMedia.ShouldBe(false);
		(await context.TelegramSessionChatBans.AnyAsync(x => x.TelegramSessionId == session.Id)).ShouldBeFalse();
	}

	[Fact]
	public async Task DisableForChatRestrictionAsync_WithoutDiscoverLink_ShouldFindChannelByChatId()
	{
		var session = await new TelegramSessionBuilder(context).CreateAsync();
		var chatId = Random.Shared.NextInt64(1_000_000, long.MaxValue);
		var discovered = await CreateDiscoveredChannelAsync(chatId);
		var destination = await CreateDestinationAsync(session.Id, chatId);

		await sut.DisableForChatRestrictionAsync(destination.Id, false, null, CancellationToken.None);

		var channel = await context.DiscoveredChannels.AsNoTracking().SingleAsync(x => x.Id == discovered.Id);
		channel.CanSendMessages.ShouldBe(false);
		channel.CanSendMedia.ShouldBe(true);
	}

	[Fact]
	public async Task UpdateDestinationStatusAsync_WithBannedStatus_ShouldDeactivateDestination()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var session = await new TelegramSessionBuilder(context).CreateAsync();
		var settings = await new RepostSettingsBuilder(context)
			.WithScheduleId(schedule.Id)
			.WithTelegramSessionId(session.Id)
			.CreateAsync();
		var destination = await new RepostDestinationBuilder(context)
			.WithRepostSettingsId(settings.Id)
			.WithIsActive(true)
			.CreateAsync();

		await sut.UpdateDestinationStatusAsync(destination.Id, ChatStatus.Banned, false, CancellationToken.None);

		var updated = await context.Set<RepostDestination>()
			.AsNoTracking()
			.FirstAsync(x => x.Id == destination.Id);
		updated.ChatStatus.ShouldBe(ChatStatus.Banned);
		updated.IsActive.ShouldBeFalse();
		updated.InfoUpdatedAt.ShouldNotBeNull();
	}

	[Fact]
	public async Task CreateRepostLogsAsync_ShouldCreateMultipleLogsForSameMessage()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var msg = new Message
		{
			Id = Guid.NewGuid(),
			ScheduleId = schedule.Id,
			TimePosting = DateTimeOffset.UtcNow.AddMinutes(1),
			Status = MessageStatus.Send,
			TextMessage = "test",
			IsTextMessage = false
		};
		await context.Messages.AddAsync(msg);
		await context.SaveChangesAsync();

		var session = await new TelegramSessionBuilder(context).CreateAsync();
		var settings = await new RepostSettingsBuilder(context)
			.WithScheduleId(schedule.Id)
			.WithTelegramSessionId(session.Id)
			.CreateAsync();
		var channel1 = 14123414421;
		var channel2 = 356356565;
		var destination1 = await new RepostDestinationBuilder(context)
			.WithRepostSettingsId(settings.Id)
			.WithChatIdentifier(channel1)
			.CreateAsync();

		var destination2 = await new RepostDestinationBuilder(context)
			.WithRepostSettingsId(settings.Id)
			.WithChatIdentifier(channel2)
			.CreateAsync();

		await sut.CreateRepostLogsAsync([
			new RepostLogEntry
			{
				MessageId = msg.Id,
				RepostDestinationId = destination1.Id,
				Status = RepostStatus.Success,
				Reason = RepostLogReason.None,
				TelegramMessageId = 111
			},
			new RepostLogEntry
			{
				MessageId = msg.Id,
				RepostDestinationId = destination2.Id,
				Status = RepostStatus.Success,
				Reason = RepostLogReason.None,
				TelegramMessageId = 222
			}
		], CancellationToken.None);

		var logs = await context.Set<RepostLog>()
			.Where(l => l.MessageId == msg.Id)
			.ToListAsync();

		logs.Count.ShouldBe(2);
		logs.ShouldContain(l => l.RepostDestinationId == destination1.Id && l.TelegramMessageId == 111);
		logs.ShouldContain(l => l.RepostDestinationId == destination2.Id && l.TelegramMessageId == 222);
	}

	[Fact]
	public async Task CreateRepostLogsAsync_WithSkippedEntry_ShouldCreateLogWithoutRepostedAt()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var msg = new Message
		{
			Id = Guid.NewGuid(),
			ScheduleId = schedule.Id,
			TimePosting = DateTimeOffset.UtcNow.AddMinutes(1),
			Status = MessageStatus.Send,
			TextMessage = "test",
			IsTextMessage = false
		};
		await context.Messages.AddAsync(msg);
		await context.SaveChangesAsync();

		var session = await new TelegramSessionBuilder(context).CreateAsync();
		var settings = await new RepostSettingsBuilder(context)
			.WithScheduleId(schedule.Id)
			.WithTelegramSessionId(session.Id)
			.CreateAsync();
		var destination = await new RepostDestinationBuilder(context)
			.WithRepostSettingsId(settings.Id)
			.CreateAsync();

		await sut.CreateRepostLogsAsync([
			new RepostLogEntry
			{
				MessageId = msg.Id,
				RepostDestinationId = destination.Id,
				Status = RepostStatus.Skipped,
				Reason = RepostLogReason.DailyLimit,
				Error = "Достигнут дневной лимит 5 репостов"
			}
		], CancellationToken.None);

		var log = await context.Set<RepostLog>()
			.FirstOrDefaultAsync(l => l.MessageId == msg.Id && l.RepostDestinationId == destination.Id);

		log.ShouldNotBeNull();
		log.Status.ShouldBe(RepostStatus.Skipped);
		log.Reason.ShouldBe(RepostLogReason.DailyLimit);
		log.RepostedAt.ShouldBeNull();
		log.TelegramMessageId.ShouldBeNull();
		log.Error.ShouldBe("Достигнут дневной лимит 5 репостов");
	}

	[Fact]
	public async Task CreateRepostLogsAsync_WithTooLongError_ShouldTruncateToColumnLength()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var msg = new Message
		{
			Id = Guid.NewGuid(),
			ScheduleId = schedule.Id,
			TimePosting = DateTimeOffset.UtcNow.AddMinutes(1),
			Status = MessageStatus.Send,
			TextMessage = "test",
			IsTextMessage = false
		};
		await context.Messages.AddAsync(msg);
		await context.SaveChangesAsync();

		var session = await new TelegramSessionBuilder(context).CreateAsync();
		var settings = await new RepostSettingsBuilder(context)
			.WithScheduleId(schedule.Id)
			.WithTelegramSessionId(session.Id)
			.CreateAsync();
		var destination = await new RepostDestinationBuilder(context)
			.WithRepostSettingsId(settings.Id)
			.CreateAsync();

		await sut.CreateRepostLogsAsync([
			new RepostLogEntry
			{
				MessageId = msg.Id,
				RepostDestinationId = destination.Id,
				Status = RepostStatus.Failed,
				Reason = RepostLogReason.ForwardFailed,
				Error = new string('x', 5000)
			}
		], CancellationToken.None);

		var log = await context.Set<RepostLog>()
			.FirstOrDefaultAsync(l => l.MessageId == msg.Id && l.RepostDestinationId == destination.Id);

		log.ShouldNotBeNull();
		log.Error!.Length.ShouldBe(2000);
	}

	[Fact]
	public async Task CreateRepostLogsAsync_WithEmptyEntries_ShouldNotCreateLogs()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var msg = new Message
		{
			Id = Guid.NewGuid(),
			ScheduleId = schedule.Id,
			TimePosting = DateTimeOffset.UtcNow.AddMinutes(1),
			Status = MessageStatus.Send,
			TextMessage = "test",
			IsTextMessage = false
		};
		await context.Messages.AddAsync(msg);
		await context.SaveChangesAsync();

		await sut.CreateRepostLogsAsync([], CancellationToken.None);

		var logs = await context.Set<RepostLog>()
			.Where(l => l.MessageId == msg.Id)
			.ToListAsync();

		logs.ShouldBeEmpty();
	}

	private async Task<RepostDestination> CreateDestinationAsync(
		Guid sessionId,
		long chatId,
		Guid? discoveredChannelId = null
	)
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var settings = await new RepostSettingsBuilder(context)
			.WithScheduleId(schedule.Id)
			.WithTelegramSessionId(sessionId)
			.CreateAsync();
		var destination = new RepostDestinationBuilder(context)
			.WithRepostSettingsId(settings.Id)
			.WithChatIdentifier(chatId)
			.WithIsActive(true)
			.WithChatStatus(ChatStatus.Active)
			.Build();
		destination.DiscoveredChannelId = discoveredChannelId;

		await context.AddAsync(destination);
		await context.SaveChangesAsync();

		return destination;
	}

	private async Task<DiscoveredChannel> CreateDiscoveredChannelAsync(long telegramId)
	{
		var channel = new DiscoveredChannel
		{
			Id = Guid.NewGuid(),
			TelegramId = telegramId,
			Status = DiscoveryStatus.Completed,
			CanSendMessages = true,
			CanSendMedia = true
		};

		await context.DiscoveredChannels.AddAsync(channel);
		await context.SaveChangesAsync();

		return channel;
	}

	private Task<RepostDestination> LoadDestinationAsync(Guid id) =>
		context.Set<RepostDestination>().AsNoTracking().FirstAsync(x => x.Id == id);
}
