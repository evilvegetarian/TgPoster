using Bogus;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
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
            UserName = new UserName(faker.Person.UserName),
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
            TelegramBotId = telegramBot.Id
        };
        await context.Schedules.AddAsync(schedule);
        await context.SaveChangesAsync();
        return schedule;
    }

    public async Task<TelegramBot> CreateTelegramBotAsync(Guid? userId)
    {
        var user = userId ?? (await CreateUserAsync()).Id;
        var telegramBot = new TelegramBot
        {
            Id = Guid.NewGuid(),
            Name = faker.Company.CompanyName(),
            ApiTelegram = "api",
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
        var randomTimes = Enumerable.Range(0, 10)
            .Select(_ => TimeOnly.FromDateTime(faker.Date.Between(
                new DateTime(1, 1, 1, 0, 0, 0),
                new DateTime(1, 1, 1, 23, 59, 59)
            ))).ToList();
        var day = new Day
        {
            Id = Guid.NewGuid(),
            ScheduleId = schedule.Id,
            DayOfWeek = faker.Random.Enum<DayOfWeek>(),
            TimePostings = randomTimes
        };
        await context.Days.AddAsync(day);
        await context.SaveChangesAsync();
        return day;
    }
}