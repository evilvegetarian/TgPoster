using Microsoft.EntityFrameworkCore;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.VO;

namespace TgPoster.Endpoint.Tests.Seeder;

internal class UserSeeder(PosterContext context) : BaseSeeder(context)
{
    public override async Task Seed()
    {
        if (await context.Users.AnyAsync())
            return;
        var user = new User
        {
            Id = GlobalConst.UserId,
            UserName = new UserName("KuperSan"),
            PasswordHash = "PasswordHash"
        };
        await context.Users.AddRangeAsync(user);
    }
}