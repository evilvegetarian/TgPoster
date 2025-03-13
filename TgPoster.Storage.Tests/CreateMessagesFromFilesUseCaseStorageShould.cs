using Microsoft.EntityFrameworkCore;
using Shouldly;
using TgPoster.Domain.Services;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.Enum;
using TgPoster.Storage.Storages;

namespace TgPoster.Storage.Tests;

public class CreateMessagesFromFilesUseCaseStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
    private readonly PosterContext _context = fixture.GetDbContext();
    private readonly Helper _helper = new(fixture.GetDbContext());
    private readonly CreateMessagesFromFilesUseCaseStorage _sut = new(fixture.GetDbContext(), new GuidFactory());

    [Fact]
    public async Task GetTelegramBot_ShouldReturnTelegramBotDto_WhenScheduleAndUserMatch()
    {
        var user = await _helper.CreateUserAsync();
        var telegramBot = new TelegramBot
        {
            Id = Guid.Parse("0dae0ada-72aa-4b01-a6d8-75ce91e111b2"),
            ApiTelegram = "TestApiToken",
            ChatId = 123456789,
            OwnerId = user.Id,
            Name = "TestBot"
        };

        var schedule = new Schedule
        {
            Id = Guid.Parse("94e4b510-ab21-48c1-9f57-4f77865cb59b"),
            Name = "schedule",
            UserId = user.Id,
            TelegramBotId = telegramBot.Id
        };

        await _context.TelegramBots.AddAsync(telegramBot);
        await _context.Schedules.AddAsync(schedule);
        await _context.SaveChangesAsync();

        var result = await _sut.GetTelegramBot(schedule.Id, user.Id, CancellationToken.None);

        result.ShouldNotBeNull();
        result.ApiTelegram.ShouldBe(telegramBot.ApiTelegram);
        result.ChatId.ShouldBe(telegramBot.ChatId);
    }

    [Fact]
    public async Task GetTelegramBot_ShouldReturnNull_WhenScheduleNotFoundOrUserMismatch()
    {
        var user = await _helper.CreateUserAsync();

        var telegramBot = new TelegramBot
        {
            Id = Guid.Parse("73515119-d27b-4316-8b73-fc9f9731fd95"),
            ApiTelegram = "AnotherApiToken",
            ChatId = 987654321,
            OwnerId = user.Id,
            Name = "AnotherBot"
        };

        var schedule = new Schedule
        {
            Id = Guid.Parse("94e4b510-ab21-48c1-9f57-4f77865cb593"),
            Name = "schedule",
            UserId = user.Id,
            TelegramBotId = telegramBot.Id
        };

        await _context.TelegramBots.AddAsync(telegramBot);
        await _context.Schedules.AddAsync(schedule);
        await _context.SaveChangesAsync();
        var anotherUser = Guid.Parse("b498cffb-ec28-4c27-a757-3192e2064e38");
        var result = await _sut.GetTelegramBot(schedule.Id, anotherUser, CancellationToken.None);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetExistMessageTimePosting_ShouldReturnOnlyFutureRegisterMessages()
    {
        var schedule = await _helper.CreateScheduleAsync();
        var futureTime1 = DateTimeOffset.UtcNow.AddHours(1);
        var futureTime2 = DateTimeOffset.UtcNow.AddHours(2);
        var futureTime3 = DateTimeOffset.UtcNow.AddHours(5);
        var pastTime1 = DateTimeOffset.UtcNow.AddHours(-1);
        var pastTime2 = DateTimeOffset.UtcNow.AddHours(-2);

        var messages = new List<Message>
        {
            new()
            {
                Id = Guid.Parse("344863e2-9d4e-4b80-ade6-4fb9f26ff9d0"),
                ScheduleId = schedule.Id,
                TimePosting = futureTime1,
                Status = MessageStatus.Register,
                IsTextMessage = true,
                TextMessage = "This is a text message"
            },
            new()
            {
                Id = Guid.Parse("b2181baa-3347-4d62-b1d2-3a7480393133"),
                ScheduleId = schedule.Id,
                TimePosting = futureTime2,
                Status = MessageStatus.Register,
                IsTextMessage = false
            },
            new()
            {
                Id = Guid.Parse("eec8649d-8705-4e8e-9546-c5604781e1f8"),
                ScheduleId = schedule.Id,
                TimePosting = pastTime1,
                Status = MessageStatus.Register,
                IsTextMessage = false
            },
            new()
            {
                Id = Guid.Parse("a443de29-9913-4be9-a5e7-295c03f8843f"),
                ScheduleId = schedule.Id,
                TimePosting = pastTime2,
                Status = MessageStatus.Send,
                IsTextMessage = true,
                TextMessage = "This is a text message"
            },
            new()
            {
                Id = Guid.Parse("3e327f1f-000a-4142-905c-d57bc2a467cd"),
                ScheduleId = schedule.Id,
                TimePosting = futureTime3,
                Status = MessageStatus.Error,
                IsTextMessage = true,
                TextMessage = "This is a text message"
            }
        };

        await _context.Messages.AddRangeAsync(messages);
        await _context.SaveChangesAsync();

        var result = await _sut.GetExistMessageTimePosting(schedule.Id, CancellationToken.None);
        var ss = result.Select(x => x.DateTime).ToList();
        ss.Count().ShouldBe(2);
    }

    [Fact]
    public async Task GetScheduleTime_ShouldReturnDictionaryOfDayAndTimePostings()
    {
        var schedule = await _helper.CreateScheduleAsync();
        var day1 = new Day
        {
            Id = Guid.Parse("94a850f1-cdaa-4ec5-a3da-e1d6351708bd"),
            ScheduleId = schedule.Id,
            DayOfWeek = DayOfWeek.Monday,
            TimePostings = new List<TimeOnly> { new(10, 0), new(14, 0) }
        };

        var day2 = new Day
        {
            Id = Guid.Parse("1c13de87-5724-4349-9979-552409c4795b"),
            ScheduleId = schedule.Id,
            DayOfWeek = DayOfWeek.Wednesday,
            TimePostings = new List<TimeOnly> { new(9, 30) }
        };

        await _context.Days.AddRangeAsync(day1, day2);
        await _context.SaveChangesAsync();

        var result = await _sut.GetScheduleTime(schedule.Id, CancellationToken.None);

        result.Count.ShouldBe(2);
        result.ShouldContainKey(day1.DayOfWeek);
        result[day1.DayOfWeek].ShouldBe(day1.TimePostings);
        result.ShouldContainKey(day2.DayOfWeek);
        result[day2.DayOfWeek].ShouldBe(day2.TimePostings);
    }

    [Fact]
    public async Task CreateMessages_ShouldCreateMessagesWithProperFiles()
    {
        var schedule = await _helper.CreateScheduleAsync();

        var file1 = new MediaFileResult
        {
            FileId = "photo1",
            ContentType = "image/jpeg"
        };
        var file2 = new MediaFileResult
        {
            FileId = "video1",
            ContentType = "video/mp4",
            PreviewPhotoIds = ["thumb1", "thumb2"]
        };

        var postingTimes = new List<DateTimeOffset>
        {
            DateTimeOffset.UtcNow.AddHours(3),
            DateTimeOffset.UtcNow.AddHours(4)
        };

        await _sut.CreateMessages(schedule.Id, [file1, file2], postingTimes, CancellationToken.None);

        var messages = await _context.Messages
            .Include(x => x.MessageFiles)
            .Where(x => x.ScheduleId == schedule.Id)
            .ToListAsync();

        messages.Count.ShouldBe(2);

        messages.SelectMany(x => x.MessageFiles.Select(x => x.TgFileId)).ShouldContain(file1.FileId);
        messages.SelectMany(x => x.MessageFiles.Select(x => x.TgFileId)).ShouldContain(file2.FileId);
    }
}