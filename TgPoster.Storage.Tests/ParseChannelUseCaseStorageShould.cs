using Microsoft.EntityFrameworkCore;
using Shouldly;
using TgPoster.Storage.Data;
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
        await sut.UpdateChannelParsingParametersAsync(cpp.Id, offsetId, CancellationToken.None);

        var updated = await context.ChannelParsingParameters
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == cpp.Id);
        updated!.LastParseId.ShouldBe(offsetId);
        updated.Status.ShouldBe(ParsingStatus.Waiting);
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
}