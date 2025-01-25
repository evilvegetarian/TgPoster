using Bogus;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.VO;

namespace TgPoster.Storage.Tests;

public class Helper(PosterContext context)
{
    public async Task<User> CreateUserAsync()
    {
        var faker = new Faker();
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
}