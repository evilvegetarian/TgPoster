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

    private async Task<ChannelParsingParameters> CreateChannelParsingParametersAsync(Guid? scheduleId = null)
    {
        var id = Guid.NewGuid();
        var schedule = scheduleId.HasValue
            ? await context.Schedules.FindAsync(scheduleId.Value)
            : await helper.CreateScheduleAsync();

        var cpp = new ChannelParsingParameters
        {
            Id = id,
            AvoidWords = ["spam", "ban"],
            Channel = "TestChannel",
            DeleteMedia = true,
            DeleteText = false,
            DateFrom = DateTime.UtcNow.AddDays(-1),
            LastParseId = 123,
            DateTo = DateTime.UtcNow.AddDays(1),
            NeedVerifiedPosts = true,
            ScheduleId = schedule.Id,
            Status = ParsingStatus.New,
        };

        await context.ChannelParsingParameters.AddAsync(cpp);
        await context.SaveChangesAsync();
        return cpp;
    }

    [Fact]
    public async Task CreateSchedule_WithHelper_Works()
    {
        // Act
        var schedule = await helper.CreateScheduleAsync();

        // Assert
        var dbSchedule = await context.Schedules.FindAsync(schedule.Id);
        dbSchedule.ShouldNotBeNull();
        dbSchedule.Id.ShouldBe(schedule.Id);

        var bot = await context.TelegramBots.FindAsync(schedule.TelegramBotId);
        bot.ShouldNotBeNull();

        var user = await context.Users.FindAsync(schedule.UserId);
        user.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetChannelParsingParameters_ReturnsNull_WhenNotExists()
    {
        var randomId = Guid.NewGuid();
        var result = await sut.GetChannelParsingParameters(randomId, CancellationToken.None);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetChannelParsingParameters_ReturnsParameters_WhenExists()
    {
        var schedule = await helper.CreateScheduleAsync();

        var cpp = await CreateChannelParsingParametersAsync(schedule.Id);

        var result = await sut.GetChannelParsingParameters(cpp.Id, CancellationToken.None);

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
    public async Task CreateMessages_InsertsMessages()
    {
        var schedule = await helper.CreateScheduleAsync();

        var messageText = "Test message";
        var fileId = "7c307ab7-6ea2-4b3a-bbca-c6e09b507b7e";

        var messageDto = new MessageDto
        {
            Text = messageText,
            ScheduleId = schedule.Id,
            IsNeedVerified = true,
            Media =
            [
                new MediaDto
                {
                    FileId = fileId,
                    MimeType = "image/jpeg",
                    PreviewPhotoIds = []
                },
                new MediaDto
                {
                    FileId = fileId,
                    MimeType = "video/mp4",
                    PreviewPhotoIds = ["preview1", "preview2"]
                }
            ]
        };

        await sut.CreateMessages([messageDto], CancellationToken.None);

        var message = await context.Messages
            .Include(m => m.MessageFiles)
            .FirstOrDefaultAsync(m => m.ScheduleId == schedule.Id && m.TextMessage == messageText);
        message.ShouldNotBeNull();
        message.TextMessage.ShouldBe(messageText);
        message.MessageFiles.Count.ShouldBe(messageDto.Media.Count);
        message.MessageFiles.First().TgFileId.ShouldBe(fileId);
    }

    [Fact]
    public async Task UpdateChannelParsingParameters_UpdatesStatus_And_LastParseId()
    {
        var cpp = await CreateChannelParsingParametersAsync();
        var offsetId = 777;
        await sut.UpdateChannelParsingParameters(cpp.Id, offsetId, CancellationToken.None);

        var updated = await context.ChannelParsingParameters.AsNoTracking().FirstOrDefaultAsync(x => x.Id == cpp.Id);
        updated!.LastParseId.ShouldBe(offsetId);
        updated.Status.ShouldBe(ParsingStatus.Waiting);
    }

    [Fact]
    public async Task UpdateInHandleStatus_SetsToInHandle()
    {
        var cpp = await CreateChannelParsingParametersAsync();
        await sut.UpdateInHandleStatus(cpp.Id, CancellationToken.None);
        var updated = await context.ChannelParsingParameters.AsNoTracking().FirstOrDefaultAsync(x => x.Id == cpp.Id);
        updated!.Status.ShouldBe(ParsingStatus.InHandle);
    }
}