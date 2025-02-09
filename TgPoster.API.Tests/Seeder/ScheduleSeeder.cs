using Microsoft.EntityFrameworkCore;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Endpoint.Tests.Seeder;

internal class ScheduleSeeder(PosterContext context) : BaseSeeder(context)
{
    public override async Task Seed()
    {
        if (await context.Schedules.AnyAsync())
            return;

        var schedules = new List<Schedule>
        {
            new()
            {
                Id = Guid.Parse("4af2c620-b83c-41f4-8800-1cafa16c5c19"),
                Name = "PerfectSchedule",
                UserId = GlobalConst.UserId,
                TelegramBotId = GlobalConst.TelegramNotWorkedBotId,
            },
            new()
            {
                Id = Guid.Parse("e57ba368-531c-457a-b00c-8c847f163218"),
                Name = "SecondSchedule",
                UserId = GlobalConst.UserId,
                TelegramBotId = GlobalConst.TelegramNotWorkedBotId,
            },
            new()
            {
                Id = Guid.Parse("351666a2-7847-468d-a546-3b362a5fec1f"),
                Name = "ThirdSchedule",
                UserId = GlobalConst.UserId,
                TelegramBotId = GlobalConst.TelegramNotWorkedBotId,
            }
        };

        await context.Schedules.AddRangeAsync(schedules);
    }
}