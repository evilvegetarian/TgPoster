using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.Enum;
using TgPoster.Storage.Storages;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public class SenderMessageStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context = fixture.GetDbContext();
	private readonly SenderMessageStorage sut = new(fixture.GetDbContext());

	[Fact]
	public async Task GetMessagesAsync_ShouldReturnMessagesInNext5MinutesWithRegisterStatus()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var now = DateTimeOffset.UtcNow;

		var msgInWindow = new Message
		{
			Id = Guid.NewGuid(),
			ScheduleId = schedule.Id,
			TimePosting = now.AddMinutes(3),
			Status = MessageStatus.Register,
			TextMessage = "in-window",
			IsTextMessage = false
		};
		var msgOutOfWindow = new Message
		{
			Id = Guid.NewGuid(),
			ScheduleId = schedule.Id,
			TimePosting = now.AddMinutes(10),
			Status = MessageStatus.Register,
			TextMessage = "out-window",
			IsTextMessage = false
		};
		var msgWrongStatus = new Message
		{
			Id = Guid.NewGuid(),
			ScheduleId = schedule.Id,
			TimePosting = now.AddMinutes(2),
			Status = MessageStatus.Error,
			TextMessage = "wrong-status",
			IsTextMessage = false
		};
		await context.Messages.AddRangeAsync(msgInWindow, msgOutOfWindow, msgWrongStatus);
		await context.SaveChangesAsync();

		var result = await sut.GetMessagesAsync();

		result.ShouldNotBeNull();
		result.Count.ShouldBeGreaterThanOrEqualTo(1);
		result.SelectMany(x => x.MessageDto).Any(m => m.Id == msgInWindow.Id).ShouldBeTrue();
		result.SelectMany(x => x.MessageDto).Any(m => m.Id == msgOutOfWindow.Id).ShouldBeFalse();
		result.SelectMany(x => x.MessageDto).Any(m => m.Id == msgWrongStatus.Id).ShouldBeFalse();
	}

	[Fact]
	public async Task UpdateStatusInHandleMessageAsync_ShouldUpdateStatusForGivenIds()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var msg = new Message
		{
			Id = Guid.NewGuid(),
			ScheduleId = schedule.Id,
			TimePosting = DateTimeOffset.UtcNow.AddMinutes(1),
			Status = MessageStatus.Register,
			TextMessage = "to-inhandle",
			IsTextMessage = false
		};
		await context.Messages.AddAsync(msg);
		await context.SaveChangesAsync();

		await sut.UpdateStatusInHandleMessageAsync([msg.Id]);
		await context.Entry(msg).ReloadAsync();

		msg.Status.ShouldBe(MessageStatus.InHandle);
	}

	[Fact]
	public async Task UpdateStatusInHandleMessageAsync_WithNonExistentId_ShouldNotThrow()
	{
		var nonExistentId = Guid.NewGuid();
		await Should.NotThrowAsync(() => sut.UpdateStatusInHandleMessageAsync([nonExistentId]));
	}

	[Fact]
	public async Task UpdateStatusInHandleMessageAsync_WithEmptyList_ShouldNotThrow()
	{
		await Should.NotThrowAsync(() => sut.UpdateStatusInHandleMessageAsync([]));
	}

	[Fact]
	public async Task UpdateStatusMessageAsync_ShouldUpdateStatusToSend()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var msg = new Message
		{
			Id = Guid.NewGuid(),
			ScheduleId = schedule.Id,
			TimePosting = DateTimeOffset.UtcNow.AddMinutes(1),
			Status = MessageStatus.Register,
			TextMessage = "to-send",
			IsTextMessage = false
		};
		await context.Messages.AddAsync(msg);
		await context.SaveChangesAsync();

		await sut.UpdateSendStatusMessageAsync(msg.Id);
		await context.Entry(msg).ReloadAsync();

		msg.Status.ShouldBe(MessageStatus.Send);
	}

	[Fact]
	public async Task UpdateStatusMessageAsync_WithNonExistentId_ShouldNotThrow()
	{
		var nonExistentId = Guid.NewGuid();
		await Should.NotThrowAsync(() => sut.UpdateSendStatusMessageAsync(nonExistentId));
	}

	[Fact]
	public async Task SaveTelegramMessageIdAsync_WithValidMessage_ShouldSaveTelegramMessageId()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var msg = new Message
		{
			Id = Guid.NewGuid(),
			ScheduleId = schedule.Id,
			TimePosting = DateTimeOffset.UtcNow.AddMinutes(1),
			Status = MessageStatus.Register,
			TextMessage = "test",
			IsTextMessage = false
		};
		await context.Messages.AddAsync(msg);
		await context.SaveChangesAsync();

		var telegramMessageId = 12345;

		await sut.SaveTelegramMessageIdAsync(msg.Id, telegramMessageId, CancellationToken.None);
		await context.Entry(msg).ReloadAsync();

		msg.TelegramMessageId.ShouldBe(telegramMessageId);
	}

	[Fact]
	public async Task SaveTelegramMessageIdAsync_WithNonExistingMessage_ShouldNotThrow()
	{
		var nonExistingId = Guid.NewGuid();
		var telegramMessageId = 12345;

		await Should.NotThrowAsync(async () =>
			await sut.SaveTelegramMessageIdAsync(nonExistingId, telegramMessageId, CancellationToken.None));
	}

	[Fact]
	public async Task GetRepostSettingsForMessageAsync_WithActiveSettings_ShouldReturnSettings()
	{
		var session = await new TelegramSessionBuilder(context).CreateAsync();
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var msg = new Message
		{
			Id = Guid.NewGuid(),
			ScheduleId = schedule.Id,
			TimePosting = DateTimeOffset.UtcNow.AddMinutes(1),
			Status = MessageStatus.Register,
			TextMessage = "test",
			IsTextMessage = false
		};
		await context.Messages.AddAsync(msg);
		await context.SaveChangesAsync();
		var channel1 = 14123414421;
		var channel2 = 356356565;
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

		var result = await sut.GetRepostSettingsForMessageAsync(msg.Id, CancellationToken.None);

		result.ShouldNotBeNull();
		result.ScheduleId.ShouldBe(schedule.Id);
		result.TelegramSessionId.ShouldBe(session.Id);
		result.Destinations.Count.ShouldBe(2);
		result.Destinations.ShouldContain(d => d.ChatIdentifier == channel1);
		result.Destinations.ShouldContain(d => d.ChatIdentifier ==channel1);
	}

	[Fact]
	public async Task GetRepostSettingsForMessageAsync_WithInactiveSettings_ShouldReturnNull()
	{
		var session = await new TelegramSessionBuilder(context).CreateAsync();
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var msg = new Message
		{
			Id = Guid.NewGuid(),
			ScheduleId = schedule.Id,
			TimePosting = DateTimeOffset.UtcNow.AddMinutes(1),
			Status = MessageStatus.Register,
			TextMessage = "test",
			IsTextMessage = false
		};
		await context.Messages.AddAsync(msg);
		await context.SaveChangesAsync();

		await new RepostSettingsBuilder(context)
			.WithScheduleId(schedule.Id)
			.WithTelegramSessionId(session.Id)
			.WithIsActive(false)
			.CreateAsync();

		var result = await sut.GetRepostSettingsForMessageAsync(msg.Id, CancellationToken.None);

		result.ShouldBeNull();
	}

	[Fact]
	public async Task GetRepostSettingsForMessageAsync_WithInactiveDestinations_ShouldReturnOnlyActive()
	{
		var session = await new TelegramSessionBuilder(context).CreateAsync();
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var msg = new Message
		{
			Id = Guid.NewGuid(),
			ScheduleId = schedule.Id,
			TimePosting = DateTimeOffset.UtcNow.AddMinutes(1),
			Status = MessageStatus.Register,
			TextMessage = "test",
			IsTextMessage = false
		};
		await context.Messages.AddAsync(msg);
		await context.SaveChangesAsync();
		var active = 141234142421;
		var inactive = 3563356565;
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

		var result = await sut.GetRepostSettingsForMessageAsync(msg.Id, CancellationToken.None);

		result.ShouldNotBeNull();
		result.Destinations.Count.ShouldBe(1);
		result.Destinations[0].ChatIdentifier.ShouldBe(active);
	}

	[Fact]
	public async Task GetRepostSettingsForMessageAsync_WithNoRepostSettings_ShouldReturnNull()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var msg = new Message
		{
			Id = Guid.NewGuid(),
			ScheduleId = schedule.Id,
			TimePosting = DateTimeOffset.UtcNow.AddMinutes(1),
			Status = MessageStatus.Register,
			TextMessage = "test",
			IsTextMessage = false
		};
		await context.Messages.AddAsync(msg);
		await context.SaveChangesAsync();

		var result = await sut.GetRepostSettingsForMessageAsync(msg.Id, CancellationToken.None);

		result.ShouldBeNull();
	}

	[Fact]
	public async Task GetRepostSettingsForMessageAsync_WithNonExistingMessage_ShouldReturnNull()
	{
		var nonExistingId = Guid.NewGuid();

		var result = await sut.GetRepostSettingsForMessageAsync(nonExistingId, CancellationToken.None);

		result.ShouldBeNull();
	}
}