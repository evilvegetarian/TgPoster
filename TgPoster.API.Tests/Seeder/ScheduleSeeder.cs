using Microsoft.EntityFrameworkCore;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Endpoint.Tests.Seeder;

internal class ScheduleSeeder(PosterContext context) : BaseSeeder
{
    public override async Task Seed()
    {
        if (await context.Schedules.AnyAsync())
        {
            return;
        }

        var schedules = new List<Schedule>
        {
            new()
            {
                Id = GlobalConst.ScheduleId,
                Name = "PerfectSchedule",
                UserId = GlobalConst.UserId,
                TelegramBotId = GlobalConst.TelegramBotId,
                ChannelId = GlobalConst.ChannelId,
                ChannelName = "PerfectChannel"
            },
            new()
            {
                Id = Guid.Parse("e57ba368-531c-457a-b00c-8c847f163218"),
                Name = "SecondSchedule",
                UserId = GlobalConst.UserId,
                TelegramBotId = GlobalConst.TelegramBotId,
                ChannelId = GlobalConst.ChannelId,
                ChannelName = "Second Channel"
            },
            new()
            {
                Id = GlobalConst.Worked.ScheduleId,
                Name = "Schedule For Test",
                UserId = GlobalConst.Worked.UserId,
                TelegramBotId = GlobalConst.Worked.TelegramBotId,
                ChannelId = GlobalConst.Worked.ChannelId,
                ChannelName = "Schedule For Test"
            }
        };

        await context.Schedules.AddRangeAsync(schedules);
    }
}