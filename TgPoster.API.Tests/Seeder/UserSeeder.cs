using Microsoft.EntityFrameworkCore;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.VO;

namespace TgPoster.Endpoint.Tests.Seeder;

internal class UserSeeder(PosterContext context) : BaseSeeder()
{
    public override async Task Seed()
    {
        if (await context.Users.AnyAsync())
            return;
        var user = new User
        {
            Id = GlobalConst.UserId,
            UserName = new UserName("Kuper"),
            PasswordHash = "PasswordHash",
        };

        var defaultUser = new User
        {
            Id = GlobalConst.UserDefaultId,
            PasswordHash = "DefaultPasswordHash",
            UserName = new UserName("Default_User_For_APP")
        };
        await context.Users.AddRangeAsync(user, defaultUser);
    }
}