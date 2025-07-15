using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Storages;

namespace TgPoster.Storage.Tests.Tests;

public class GetMessageStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
    private readonly PosterContext context = fixture.GetDbContext();
    private readonly Helper helper = new(fixture.GetDbContext());
    private readonly GetMessageStorage sut = new(fixture.GetDbContext());

    [Fact]
    public async Task GetMessagesAsync_WithExistingMessage_ShouldReturnMessage()
    {
        var schedule = await helper.CreateScheduleAsync();
        var message = await helper.CreateMessageAsync(schedule.Id);
        var messageFile = await helper.CreateMessageFileAsync(message.Id);

        var result = await sut.GetMessagesAsync(message.Id, schedule.UserId, CancellationToken.None);

        result.ShouldNotBeNull();
        result.Id.ShouldBe(message.Id);
        result.TextMessage.ShouldBe(message.TextMessage);
        result.ScheduleId.ShouldBe(message.ScheduleId);
        //result.TimePosting.ShouldBe(message.TimePosting);
        result.Files.ShouldNotBeEmpty();
        result.Files.Count.ShouldBe(1);
        result.Files.First().Id.ShouldBe(messageFile.Id);
        result.Files.First().ContentType.ShouldBe(messageFile.ContentType);
        result.Files.First().TgFileId.ShouldBe(messageFile.TgFileId);
    }

    [Fact]
    public async Task GetMessagesAsync_WithVideoMessageFile_ShouldReturnVideoWithThumbnails()
    {
        var schedule = await helper.CreateScheduleAsync();
        var message = await helper.CreateMessageAsync(schedule.Id);
        var videoFile = await helper.CreateVideoMessageFileAsync(message.Id);

        var result = await sut.GetMessagesAsync(message.Id, schedule.UserId, CancellationToken.None);

        result.ShouldNotBeNull();
        result.Files.ShouldNotBeEmpty();
        var returnedFile = result.Files.First();
        returnedFile.ContentType.ShouldBe("video/mp4");
        returnedFile.PreviewIds.ShouldNotBeEmpty();
        returnedFile.PreviewIds.Count.ShouldBe(2);
        returnedFile.PreviewIds.ShouldBe(videoFile.ThumbnailIds.ToList());
    }

    [Fact]
    public async Task GetMessagesAsync_WithNonExistingMessage_ShouldReturnNull()
    {
        var user = await helper.CreateUserAsync();
        var nonExistingMessageId = Guid.NewGuid();

        var result = await sut.GetMessagesAsync(nonExistingMessageId, user.Id, CancellationToken.None);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetMessagesAsync_WithWrongUserId_ShouldReturnNull()
    {
        var schedule = await helper.CreateScheduleAsync();
        var message = await helper.CreateMessageAsync(schedule.Id);
        var wrongUserId = Guid.NewGuid();

        var result = await sut.GetMessagesAsync(message.Id, wrongUserId, CancellationToken.None);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetMessagesAsync_WithMessageWithoutFiles_ShouldReturnMessageWithEmptyFiles()
    {
        var schedule = await helper.CreateScheduleAsync();
        var message = await helper.CreateMessageAsync(schedule.Id);

        var result = await sut.GetMessagesAsync(message.Id, schedule.UserId, CancellationToken.None);

        result.ShouldNotBeNull();
        result.Id.ShouldBe(message.Id);
        result.Files.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetMessagesAsync_WithMultipleFiles_ShouldReturnAllFiles()
    {
        var schedule = await helper.CreateScheduleAsync();
        var message = await helper.CreateMessageAsync(schedule.Id);
        var imageFile = await helper.CreateMessageFileAsync(message.Id, "image/jpeg");
        var videoFile = await helper.CreateVideoMessageFileAsync(message.Id);

        var result = await sut.GetMessagesAsync(message.Id, schedule.UserId, CancellationToken.None);

        result.ShouldNotBeNull();
        result.Files.Count.ShouldBe(2);
        result.Files.ShouldContain(x => x.Id == imageFile.Id && x.ContentType == "image/jpeg");
        result.Files.ShouldContain(x => x.Id == videoFile.Id && x.ContentType == "video/mp4");
    }
}
