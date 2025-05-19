using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.VO;

namespace TgPoster.Endpoint.Tests.Seeder;

internal class UserSeeder(PosterContext context, string hash) : BaseSeeder
{
    public override async Task Seed()
    {
        if (await context.Users.AnyAsync())
        {
            return;
        }

        var user = new User
        {
            Id = GlobalConst.UserId,
            UserName = new UserName("Kuper"),
            PasswordHash = "PasswordHash"
        };

        var defaultUser = new User
        {
            Id = GlobalConst.Worked.UserId,
            PasswordHash = hash,
            UserName = new UserName(GlobalConst.Worked.UserName)
        };

        await context.Users.AddRangeAsync(user, defaultUser);
    }
}