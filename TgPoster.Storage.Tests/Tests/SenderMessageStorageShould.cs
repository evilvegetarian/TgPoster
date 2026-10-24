using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.Enum;
using TgPoster.Storage.Storages;
using TgPoster.Storage.Tests.Builders;
using TgPoster.Worker.Domain.UseCases.SenderMessageWorker;

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
		result.SelectMany(x => x.MessageDto).Single(m => m.Id == msgInWindow.Id).ScheduleId.ShouldBe(schedule.Id);
		result.SelectMany(x => x.MessageDto).Any(m => m.Id == msgOutOfWindow.Id).ShouldBeFalse();
		result.SelectMany(x => x.MessageDto).Any(m => m.Id == msgWrongStatus.Id).ShouldBeFalse();
	}

	[Fact]
	public async Task GetMessagesAsync_ShouldSkipUnverifiedMessages()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var verifiedSchedule = await new ScheduleBuilder(context).CreateAsync();
		var now = DateTimeOffset.UtcNow;
		var unverified = CreateMessage(schedule.Id, now.AddMinutes(3), MessageStatus.Register);
		unverified.IsVerified = false;
		var verified = CreateMessage(verifiedSchedule.Id, now.AddMinutes(3), MessageStatus.Register);
		var unverifiedInVerifiedSchedule = CreateMessage(verifiedSchedule.Id, now.AddMinutes(4), MessageStatus.Register);
		unverifiedInVerifiedSchedule.IsVerified = false;
		await context.Messages.AddRangeAsync(unverified, verified, unverifiedInVerifiedSchedule);
		await context.SaveChangesAsync();

		var result = await sut.GetMessagesAsync();

		result.ShouldNotContain(x => x.ChannelId == schedule.ChannelId);
		var messageIds = result.SelectMany(x => x.MessageDto).Select(x => x.Id).ToList();
		messageIds.ShouldContain(verified.Id);
		messageIds.ShouldNotContain(unverified.Id);
		messageIds.ShouldNotContain(unverifiedInVerifiedSchedule.Id);
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
	public async Task SaveTelegramMessageIdsAsync_WithValidMessage_ShouldSaveAllIds()
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

		await sut.SaveTelegramMessageIdsAsync(msg.Id, 12345, [12345, 12346, 12347], 12347, CancellationToken.None);
		await context.Entry(msg).ReloadAsync();

		msg.TelegramMessageId.ShouldBe(12345);
		msg.TelegramMessageIds.ShouldBe([12345, 12346, 12347]);
		msg.TelegramTextMessageId.ShouldBe(12347);
	}

	[Fact]
	public async Task SaveTelegramMessageIdsAsync_WithNonExistingMessage_ShouldNotThrow()
	{
		var nonExistingId = Guid.NewGuid();

		await Should.NotThrowAsync(async () =>
			await sut.SaveTelegramMessageIdsAsync(nonExistingId, 12345, [12345], 12345, CancellationToken.None));
	}

	[Fact]
	public async Task UpdateErrorStatusMessageAsync_ShouldUpdateStatusToError()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var msg = new Message
		{
			Id = Guid.NewGuid(),
			ScheduleId = schedule.Id,
			TimePosting = DateTimeOffset.UtcNow.AddMinutes(1),
			Status = MessageStatus.InHandle,
			TextMessage = "to-error",
			IsTextMessage = false
		};
		await context.Messages.AddAsync(msg);
		await context.SaveChangesAsync();

		await sut.UpdateErrorStatusMessageAsync(msg.Id, "403: Forbidden: bot was kicked", CancellationToken.None);
		await context.Entry(msg).ReloadAsync();

		msg.Status.ShouldBe(MessageStatus.Error);
		msg.SendError.ShouldBe("403: Forbidden: bot was kicked");
	}

	[Fact]
	public async Task UpdateErrorStatusMessageAsync_WithLongError_ShouldTruncateTo1000()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var msg = new Message
		{
			Id = Guid.NewGuid(),
			ScheduleId = schedule.Id,
			TimePosting = DateTimeOffset.UtcNow.AddMinutes(1),
			Status = MessageStatus.InHandle,
			TextMessage = "long-error",
			IsTextMessage = false
		};
		await context.Messages.AddAsync(msg);
		await context.SaveChangesAsync();
		var longError = new string('x', 1500);

		await sut.UpdateErrorStatusMessageAsync(msg.Id, longError, CancellationToken.None);
		await context.Entry(msg).ReloadAsync();

		msg.Status.ShouldBe(MessageStatus.Error);
		msg.SendError.ShouldBe(longError[..1000]);
	}

	[Fact]
	public async Task UpdateErrorStatusMessageAsync_WithNonExistentId_ShouldNotThrow()
	{
		var nonExistentId = Guid.NewGuid();
		await Should.NotThrowAsync(() =>
			sut.UpdateErrorStatusMessageAsync(nonExistentId, "error", CancellationToken.None));
	}

	[Fact]
	public async Task RecoverInHandleMessagesAsync_ShouldRequeueFutureAndFailPastInHandleMessages()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var now = DateTimeOffset.UtcNow;
		var future = CreateMessage(schedule.Id, now.AddMinutes(3), MessageStatus.InHandle);
		var past = CreateMessage(schedule.Id, now.AddMinutes(-3), MessageStatus.InHandle);
		var registered = CreateMessage(schedule.Id, now.AddMinutes(-3), MessageStatus.Register);
		var sent = CreateMessage(schedule.Id, now.AddMinutes(-3), MessageStatus.Send);
		await context.Messages.AddRangeAsync(future, past, registered, sent);
		await context.SaveChangesAsync();

		var result = await sut.RecoverInHandleMessagesAsync(now, "interrupted", CancellationToken.None);
		await context.Entry(future).ReloadAsync();
		await context.Entry(past).ReloadAsync();
		await context.Entry(registered).ReloadAsync();
		await context.Entry(sent).ReloadAsync();

		future.Status.ShouldBe(MessageStatus.Register);
		future.SendError.ShouldBeNull();
		past.Status.ShouldBe(MessageStatus.Error);
		past.SendError.ShouldBe("interrupted");
		registered.Status.ShouldBe(MessageStatus.Register);
		registered.SendError.ShouldBeNull();
		sent.Status.ShouldBe(MessageStatus.Send);
		sent.SendError.ShouldBeNull();
		result.ShouldContain(new InterruptedMessage(past.Id, schedule.Id));
		result.ShouldNotContain(x => x.MessageId == future.Id);
		result.ShouldNotContain(x => x.MessageId == registered.Id);
		result.ShouldNotContain(x => x.MessageId == sent.Id);
	}

	[Fact]
	public async Task UpdateSendStatusMessageAsync_ShouldClearSendError()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var msg = CreateMessage(schedule.Id, DateTimeOffset.UtcNow.AddMinutes(1), MessageStatus.Error);
		msg.SendError = "previous error";
		await context.Messages.AddAsync(msg);
		await context.SaveChangesAsync();

		await sut.UpdateSendStatusMessageAsync(msg.Id);
		await context.Entry(msg).ReloadAsync();

		msg.Status.ShouldBe(MessageStatus.Send);
		msg.SendError.ShouldBeNull();
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
		result.Count.ShouldBe(1);
		result[0].ScheduleId.ShouldBe(schedule.Id);
		result[0].TelegramSessionId.ShouldBe(session.Id);
		result[0].Destinations.Count.ShouldBe(2);
		result[0].Destinations.ShouldContain(d => d.ChatIdentifier == channel1);
		result[0].Destinations.ShouldContain(d => d.ChatIdentifier == channel2);
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

		result.ShouldBeEmpty();
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
		result.Count.ShouldBe(1);
		result[0].Destinations.Count.ShouldBe(1);
		result[0].Destinations[0].ChatIdentifier.ShouldBe(active);
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

		result.ShouldBeEmpty();
	}

	[Fact]
	public async Task GetRepostSettingsForMessageAsync_WithNonExistingMessage_ShouldReturnNull()
	{
		var nonExistingId = Guid.NewGuid();

		var result = await sut.GetRepostSettingsForMessageAsync(nonExistingId, CancellationToken.None);

		result.ShouldBeEmpty();
	}

	private static Message CreateMessage(Guid scheduleId, DateTimeOffset timePosting, MessageStatus status) =>
		new()
		{
			Id = Guid.NewGuid(),
			ScheduleId = scheduleId,
			TimePosting = timePosting,
			Status = status,
			TextMessage = status.ToString(),
			IsTextMessage = false
		};
}