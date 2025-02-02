using Microsoft.EntityFrameworkCore;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.VO;

namespace TgPoster.Endpoint.Tests.Seeder;

internal abstract class BaseSeeder(PosterContext context)
{
    public abstract Task Seed();
}

internal class ScheduleSeeder(PosterContext context) : BaseSeeder(context)
{
    public override async Task Seed()
    {
        if (await context.Schedules.AnyAsync())
            return;

        var user = new User
        {
            Id = Guid.Parse("a198d4fb-60ea-4712-bb67-e0aad51715f1"),
            UserName = new UserName("KuperSan"),
            PasswordHash = "PasswordHash"
        };

        var schedules = new List<Schedule>
        {
            new()
            {
                Id = Guid.Parse("4af2c620-b83c-41f4-8800-1cafa16c5c19"),
                Name = "PerfectSchedule",
                UserId = user.Id,
            },
            new()
            {
                Id = Guid.Parse("e57ba368-531c-457a-b00c-8c847f163218"),
                Name = "SecondSchedule",
                UserId = user.Id,
            },
            new()
            {
                Id = Guid.Parse("351666a2-7847-468d-a546-3b362a5fec1f"),
                Name = "ThirdSchedule",
                UserId = user.Id,
            }
        };

        await context.Users.AddAsync(user);
        await context.Schedules.AddRangeAsync(schedules);
    }
}