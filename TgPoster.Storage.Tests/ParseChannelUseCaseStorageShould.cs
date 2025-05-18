using Microsoft.EntityFrameworkCore;
using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.Enum;
using TgPoster.Storage.Storages;
using TgPoster.Worker.Domain.UseCases.ParseChannel;

namespace TgPoster.Storage.Tests;

public class ParseChannelUseCaseStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
    private readonly PosterContext context = fixture.GetDbContext();
    private readonly Helper helper = new(fixture.GetDbContext());
    private readonly ParseChannelUseCaseStorage sut = new(fixture.GetDbContext(), new GuidFactory());

    [Fact]
    public async Task GetChannelParsingParameters_WithNotExistId_ShouldReturnsNull()
    {
        var randomId = Guid.NewGuid();
        var result = await sut.GetChannelParsingParametersAsync(randomId, CancellationToken.None);
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetChannelParsingParameters_WithExistId_ShouldReturnsParameters()
    {
        var schedule = await helper.CreateScheduleAsync();

        var cpp = await helper.CreateChannelParsingParametersAsync(schedule.Id);

        var result = await sut.GetChannelParsingParametersAsync(cpp.Id, CancellationToken.None);

        result.ShouldNotBeNull();
        result.ChannelName.ShouldBe(cpp.Channel);
        result.Token.ShouldNotBeNullOrWhiteSpace();
        result.ScheduleId.ShouldBe(schedule.Id);
        result.AvoidWords.ShouldBe(cpp.AvoidWords);
        result.IsNeedVerified.ShouldBeTrue();
        result.DeleteMedia.ShouldBe(cpp.DeleteMedia);
        result.DeleteText.ShouldBe(cpp.DeleteText);
        result.LastParsedId.ShouldBe(cpp.LastParseId);
    }

    [Fact]
    public async Task CreateMessages_WithValidData_ShouldInsertsMessages()
    {
        var schedule = await helper.CreateScheduleAsync();

        var messageText = "Test message";
        var photoId = "7c307ab7-6ea2-4b3a-bbca-c6e09b507b7e";
        var videoId = "2c307ab7-6ea2-4b3a-bbca-c6e09b507b7e";

        var messageDto = new MessageDto
        {
            Text = messageText,
            ScheduleId = schedule.Id,
            IsNeedVerified = true,
            Media =
            [
                new MediaDto
                {
                    FileId = photoId,
                    MimeType = "image/jpeg",
                    PreviewPhotoIds = []
                },
                new MediaDto
                {
                    FileId = videoId,
                    MimeType = "video/mp4",
                    PreviewPhotoIds = ["preview1", "preview2"]
                }
            ]
        };

        await sut.CreateMessagesAsync([messageDto], CancellationToken.None);

        var message = await context.Messages
            .Include(m => m.MessageFiles)
            .FirstOrDefaultAsync(m => m.ScheduleId == schedule.Id && m.TextMessage == messageText);
        message.ShouldNotBeNull();
        message.TextMessage.ShouldBe(messageText);
        message.MessageFiles.Count.ShouldBe(messageDto.Media.Count);
        message.MessageFiles.Count(x => x.TgFileId == photoId).ShouldBe(1);
        message.MessageFiles.Count(x => x.TgFileId == videoId).ShouldBe(1);
    }

    [Fact]
    public async Task UpdateChannelParsingParameters_WithValidData_ShouldUpdate()
    {
        var cpp = await helper.CreateChannelParsingParametersAsync();
        var offsetId = 777;
        await sut.UpdateChannelParsingParametersAsync(cpp.Id, offsetId, true, CancellationToken.None);

        var updated = await context.ChannelParsingParameters
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == cpp.Id);
        updated!.LastParseId.ShouldBe(offsetId);
        updated.Status.ShouldBe(ParsingStatus.Waiting);
    }

    [Fact]
    public async Task UpdateChannelParsingParameters_WithCheckNewPosts_ShouldParsingWaiting()
    {
        var cpp = await helper.CreateChannelParsingParametersAsync();
        await sut.UpdateChannelParsingParametersAsync(cpp.Id, int.MaxValue, true, CancellationToken.None);

        var updated = await context.ChannelParsingParameters
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == cpp.Id);
        updated!.Status.ShouldBe(ParsingStatus.Waiting);
    }

    [Fact]
    public async Task UpdateChannelParsingParameters_WithNotCheckNewPosts_ShouldParsingFinished()
    {
        var cpp = await helper.CreateChannelParsingParametersAsync();
        await sut.UpdateChannelParsingParametersAsync(cpp.Id, int.MaxValue, false, CancellationToken.None);

        var updated = await context.ChannelParsingParameters
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == cpp.Id);
        updated!.Status.ShouldBe(ParsingStatus.Finished);
    }

    [Fact]
    public async Task UpdateInHandleStatus_WithValidData_ShouldUpdate()
    {
        var cpp = await helper.CreateChannelParsingParametersAsync();
        await sut.UpdateInHandleStatusAsync(cpp.Id, CancellationToken.None);
        var updated = await context.ChannelParsingParameters
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == cpp.Id);
        updated!.Status.ShouldBe(ParsingStatus.InHandle);
    }

    [Fact]
    public async Task UpdateErrorStatus_WithValidData_ShouldUpdate()
    {
        var cpp = await helper.CreateChannelParsingParametersAsync();
        await sut.UpdateErrorStatusAsync(cpp.Id, CancellationToken.None);
        var updated = await context.ChannelParsingParameters
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == cpp.Id);
        updated!.Status.ShouldBe(ParsingStatus.Failed);
    }

    [Fact]
    public async Task GetScheduleTime_WithDays_ShouldReturnDictionary()
    {
        var day = await helper.CreateDayAsync();
        var result = await sut.GetScheduleTimeAsync(day.ScheduleId, CancellationToken.None);
        result.ShouldContainKey(day.DayOfWeek);
        day.TimePostings.All(tp => result[day.DayOfWeek].Contains(tp)).ShouldBeTrue();
    }

    [Fact]
    public async Task GetScheduleTime_WithNoDays_ShouldReturnEmptyDictionary()
    {
        var schedule = await helper.CreateScheduleAsync();
        var result = await sut.GetScheduleTimeAsync(schedule.Id, CancellationToken.None);
        result.ShouldBeEmpty();
    }

    /*[Fact]
    public async Task GetExistMessageTimePosting_ShouldReturnOnlyFutureRegistered()
    {
        var schedule = await helper.CreateScheduleAsync();
        var now = DateTimeOffset.UtcNow;

        var msgPast = new Message
        {
            Id = Guid.NewGuid(),
            ScheduleId = schedule.Id,
            TimePosting = now.AddMinutes(-10),
            Status = MessageStatus.Register,
            TextMessage = "past",
            IsTextMessage = false
        };
        var msgFuture = new Message
        {
            Id = Guid.NewGuid(),
            ScheduleId = schedule.Id,
            TimePosting = now.AddMinutes(10),
            Status = MessageStatus.Register,
            TextMessage = "future",
            IsTextMessage = false
        };
        var msgFutureNotRegistered = new Message
        {
            Id = Guid.NewGuid(),
            ScheduleId = schedule.Id,
            TimePosting = now.AddMinutes(20),
            Status = MessageStatus.Error,
            TextMessage = "future-failed",
            IsTextMessage = false
        };
        await context.Messages.AddRangeAsync(msgPast, msgFuture, msgFutureNotRegistered);
        await context.SaveChangesAsync();

        var result = await sut.GetExistMessageTimePostingAsync(schedule.Id, CancellationToken.None);
        result.ShouldContain(msgFuture.TimePosting);
        result.ShouldNotContain(msgPast.TimePosting);
        result.ShouldNotContain(msgFutureNotRegistered.TimePosting);
    }*/

    [Fact]
    public async Task GetExistMessageTimePosting_WithNoMessages_ShouldReturnEmptyList()
    {
        var schedule = await helper.CreateScheduleAsync();
        var result = await sut.GetExistMessageTimePostingAsync(schedule.Id, CancellationToken.None);
        result.ShouldBeEmpty();
    }
}