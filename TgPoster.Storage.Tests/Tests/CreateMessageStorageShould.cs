using Microsoft.EntityFrameworkCore;
using Shouldly;
using TgPoster.API.Domain.Services;
using TgPoster.API.Domain.UseCases.Messages.CreateMessage;
using TgPoster.Storage.Data;
using TgPoster.Storage.Storages;

namespace TgPoster.Storage.Tests.Tests;

public class CreateMessageStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
    private readonly PosterContext context = fixture.GetDbContext();
    private readonly Helper helper = new(fixture.GetDbContext());
    private readonly CreateMessageStorage sut = new(fixture.GetDbContext(), new GuidFactory());

    [Fact]
    public async Task ExistScheduleAsync_WithExistingSchedule_ShouldReturnTrue()
    {
        var schedule = await helper.CreateScheduleAsync();
        
        var result = await sut.ExistScheduleAsync(schedule.UserId, schedule.Id, CancellationToken.None);
        
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task ExistScheduleAsync_WithNonExistingSchedule_ShouldReturnFalse()
    {
        var user = await helper.CreateUserAsync();
        var nonExistingScheduleId = Guid.NewGuid();
        
        var result = await sut.ExistScheduleAsync(user.Id, nonExistingScheduleId, CancellationToken.None);
        
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task ExistScheduleAsync_WithWrongUserId_ShouldReturnFalse()
    {
        var schedule = await helper.CreateScheduleAsync();
        var wrongUserId = Guid.NewGuid();
        
        var result = await sut.ExistScheduleAsync(wrongUserId, schedule.Id, CancellationToken.None);
        
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task GetTelegramBotAsync_WithExistingSchedule_ShouldReturnTelegramBot()
    {
        var schedule = await helper.CreateScheduleAsync();
        
        var result = await sut.GetTelegramBotAsync(schedule.Id, schedule.UserId, CancellationToken.None);
        
        result.ShouldNotBeNull();
        result.ApiTelegram.ShouldNotBeNullOrEmpty();
        result.ChatId.ShouldNotBe(0);
    }

    [Fact]
    public async Task GetTelegramBotAsync_WithNonExistingSchedule_ShouldReturnNull()
    {
        var user = await helper.CreateUserAsync();
        var nonExistingScheduleId = Guid.NewGuid();
        
        var result = await sut.GetTelegramBotAsync(nonExistingScheduleId, user.Id, CancellationToken.None);
        
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetTelegramBotAsync_WithWrongUserId_ShouldReturnNull()
    {
        var schedule = await helper.CreateScheduleAsync();
        var wrongUserId = Guid.NewGuid();
        
        var result = await sut.GetTelegramBotAsync(schedule.Id, wrongUserId, CancellationToken.None);
        
        result.ShouldBeNull();
    }

    [Fact]
    public async Task CreateMessagesAsync_WithValidData_ShouldCreateMessage()
    {
        var schedule = await helper.CreateScheduleAsync();
        var text = "Test message";
        var time = DateTimeOffset.UtcNow.AddHours(1);
        var files = new List<MediaFileResult>
        {
            new() { FileId = "file1", MimeType = "image/jpeg" },
            new() { FileId = "file2", MimeType = "video/mp4" }
        };

        var messageId = await sut.CreateMessagesAsync(schedule.Id, text, time, files, CancellationToken.None);

        messageId.ShouldNotBe(Guid.Empty);

        var createdMessage = await context.Messages
            .Include(x => x.MessageFiles)
            .FirstOrDefaultAsync(x => x.Id == messageId);
        
        createdMessage.ShouldNotBeNull();
        createdMessage.ScheduleId.ShouldBe(schedule.Id);
        createdMessage.TextMessage.ShouldBe(text);
        createdMessage.MessageFiles.Count.ShouldBe(2);
        createdMessage.MessageFiles.ShouldContain(x => x.TgFileId == "file1" && x.ContentType == "image/jpeg");
        createdMessage.MessageFiles.ShouldContain(x => x.TgFileId == "file2" && x.ContentType == "video/mp4");
    }

    [Fact]
    public async Task CreateMessagesAsync_WithoutFiles_ShouldCreateMessageWithoutFiles()
    {
        var schedule = await helper.CreateScheduleAsync();
        var text = "Test message without files";
        var time = DateTimeOffset.UtcNow.AddHours(1);
        var files = new List<MediaFileResult>();

        var messageId = await sut.CreateMessagesAsync(schedule.Id, text, time, files, CancellationToken.None);

        messageId.ShouldNotBe(Guid.Empty);

        var createdMessage = await context.Messages
            .Include(x => x.MessageFiles)
            .FirstOrDefaultAsync(x => x.Id == messageId);
        
        createdMessage.ShouldNotBeNull();
        createdMessage.ScheduleId.ShouldBe(schedule.Id);
        createdMessage.TextMessage.ShouldBe(text); 
        //createdMessage.TimePosting.ShouldBe(time);
        createdMessage.MessageFiles.ShouldBeEmpty();
    }

    [Fact]
    public async Task CreateMessagesAsync_WithoutText_ShouldCreateMessageWithoutText()
    {
        var schedule = await helper.CreateScheduleAsync();
        var time = DateTimeOffset.UtcNow.AddHours(1);
        var files = new List<MediaFileResult>
        {
            new() { FileId = "file1", MimeType = "image/jpeg" }
        };

        var messageId = await sut.CreateMessagesAsync(schedule.Id, null, time, files, CancellationToken.None);

        messageId.ShouldNotBe(Guid.Empty);

        var createdMessage = await context.Messages
            .Include(x => x.MessageFiles)
            .FirstOrDefaultAsync(x => x.Id == messageId);
        
        createdMessage.ShouldNotBeNull();
        createdMessage.ScheduleId.ShouldBe(schedule.Id);
        createdMessage.TextMessage.ShouldBeNull();
        createdMessage.MessageFiles.Count.ShouldBe(1);
    }
}
