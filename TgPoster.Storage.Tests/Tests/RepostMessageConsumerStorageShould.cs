using Microsoft.EntityFrameworkCore;
using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.Enum;
using TgPoster.Storage.Storages.Repost;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public sealed class RepostMessageConsumerStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context = fixture.GetDbContext();
	private readonly RepostMessageConsumerStorage sut = new(fixture.GetDbContext());

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

		var result = await sut.GetRepostDataAsync(msg.Id,settings.Id, CancellationToken.None);

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

	var settings=	await new RepostSettingsBuilder(context)
			.WithScheduleId(schedule.Id)
			.WithTelegramSessionId(session.Id)
			.WithIsActive(false)
			.CreateAsync();

		var result = await sut.GetRepostDataAsync(msg.Id,settings.Id, CancellationToken.None);

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
	public async Task CreateRepostLogAsync_WithSuccess_ShouldCreateLogWithSuccessStatus()
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

		await sut.CreateRepostLogAsync(msg.Id, destination.Id, telegramMessageId, null, CancellationToken.None);

		var log = await context.Set<RepostLog>()
			.FirstOrDefaultAsync(l => l.MessageId == msg.Id && l.RepostDestinationId == destination.Id);

		log.ShouldNotBeNull();
		log.TelegramMessageId.ShouldBe(telegramMessageId);
		log.Status.ShouldBe(RepostStatus.Success);
		log.RepostedAt.ShouldNotBeNull();
		log.Error.ShouldBeNull();
	}

	[Fact]
	public async Task CreateRepostLogAsync_WithError_ShouldCreateLogWithFailedStatus()
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

		await sut.CreateRepostLogAsync(msg.Id, destination.Id, null, errorMessage, CancellationToken.None);

		var log = await context.Set<RepostLog>()
			.FirstOrDefaultAsync(l => l.MessageId == msg.Id && l.RepostDestinationId == destination.Id);

		log.ShouldNotBeNull();
		log.TelegramMessageId.ShouldBeNull();
		log.Status.ShouldBe(RepostStatus.Failed);
		log.RepostedAt.ShouldBeNull();
		log.Error.ShouldBe(errorMessage);
	}

	[Fact]
	public async Task CreateRepostLogAsync_ShouldCreateMultipleLogsForSameMessage()
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

		await sut.CreateRepostLogAsync(msg.Id, destination1.Id, 111, null, CancellationToken.None);
		await sut.CreateRepostLogAsync(msg.Id, destination2.Id, 222, null, CancellationToken.None);

		var logs = await context.Set<RepostLog>()
			.Where(l => l.MessageId == msg.Id)
			.ToListAsync();

		logs.Count.ShouldBe(2);
		logs.ShouldContain(l => l.RepostDestinationId == destination1.Id && l.TelegramMessageId == 111);
		logs.ShouldContain(l => l.RepostDestinationId == destination2.Id && l.TelegramMessageId == 222);
	}
}
