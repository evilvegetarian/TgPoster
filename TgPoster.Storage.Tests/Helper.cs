using Bogus;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.Enum;
using TgPoster.Storage.Data.VO;

namespace TgPoster.Storage.Tests;

public class Helper(PosterContext context)
{
    private readonly Faker faker = new();

    public async Task<User> CreateUserAsync()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = new UserName(faker.Internet.UserName()),
            PasswordHash = faker.Internet.Password()
        };
        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();
        return user;
    }

    public async Task<Schedule> CreateScheduleAsync()
    {
        var user = await CreateUserAsync();
        var telegramBot = await CreateTelegramBotAsync(user.Id);
        var schedule = new Schedule
        {
            Id = Guid.NewGuid(),
            Name = faker.Company.CompanyName(),
            UserId = user.Id,
            TelegramBotId = telegramBot.Id,
            ChannelId = faker.Random.Long(-100000000000000, -199999999999999)
        };
        await context.Schedules.AddAsync(schedule);
        await context.SaveChangesAsync();
        return schedule;
    }

    private async Task<TelegramBot> CreateTelegramBotAsync(Guid? userId)
    {
        var user = userId ?? (await CreateUserAsync()).Id;
        var telegramBot = new TelegramBot
        {
            Id = Guid.NewGuid(),
            Name = faker.Company.CompanyName(),
            ApiTelegram = "api-26203bf7-31a4-4f9d-b262-bb48b45b24c5",
            ChatId = faker.Random.Long(),
            OwnerId = user
        };
        await context.TelegramBots.AddAsync(telegramBot);
        await context.SaveChangesAsync();
        return telegramBot;
    }

    public async Task<Day> CreateDayAsync()
    {
        var schedule = await CreateScheduleAsync();
        return await CreateDayAsync(schedule.Id);
    }

    public async Task<Day> CreateDayAsync(Guid scheduleId)
    {
        var randomTimes = Enumerable.Range(0, 10)
            .Select(_ => TimeOnly.FromDateTime(faker.Date.Between(
                new DateTime(1, 1, 1, 0, 0, 0),
                new DateTime(1, 1, 1, 23, 59, 59)
            ))).ToList();
        var day = new Day
        {
            Id = Guid.NewGuid(),
            ScheduleId = scheduleId,
            DayOfWeek = faker.Random.Enum<DayOfWeek>(),
            TimePostings = randomTimes
        };
        await context.Days.AddAsync(day);
        await context.SaveChangesAsync();
        return day;
    }


    public async Task<ChannelParsingParameters> CreateChannelParsingParametersAsync(
        Guid? scheduleId = null,
        ParsingStatus status = ParsingStatus.New,
        bool checkNewPosts = false
    )
    {
        var id = Guid.NewGuid();
        var schedule = scheduleId.HasValue
            ? await context.Schedules.FindAsync(scheduleId.Value)
            : await CreateScheduleAsync();

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
            ScheduleId = schedule!.Id,
            Status = status,
            CheckNewPosts = checkNewPosts
        };

        await context.ChannelParsingParameters.AddAsync(cpp);
        await context.SaveChangesAsync();
        return cpp;
    }
}