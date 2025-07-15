using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.Enum;
using TgPoster.Storage.Storages;

namespace TgPoster.Storage.Tests.Tests;

public class SenderMessageStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
    private readonly PosterContext context = fixture.GetDbContext();
    private readonly Helper helper = new(fixture.GetDbContext());
    private readonly SenderMessageStorage sut = new(fixture.GetDbContext());

    [Fact]
    public async Task GetMessagesAsync_ShouldReturnMessagesInNext5MinutesWithRegisterStatus()
    {
        var schedule = await helper.CreateScheduleAsync();
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
        var schedule = await helper.CreateScheduleAsync();
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
        var schedule = await helper.CreateScheduleAsync();
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
}