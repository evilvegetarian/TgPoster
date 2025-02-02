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
        var schedule = new Schedule
        {
            Id = Guid.NewGuid(),
            Name = faker.Company.CompanyName(),
            UserId = user.Id,
        };
        await context.Schedules.AddAsync(schedule);
        await context.SaveChangesAsync();
        return schedule;
    }
}