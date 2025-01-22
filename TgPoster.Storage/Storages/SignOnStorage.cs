using TgPoster.Domain.UseCases.SignOn;
using TgPoster.Storage.Data;
using TgPoster.Storage.Entities;

namespace TgPoster.Storage.Storages;

internal sealed class SignOnStorage(PosterContext context, IGuidFactory guidFactory) : ISignOnStorage
{
    public async Task<Guid> CreateUserAsync(string username, string password)
    {
        var user = new User
        {
            Id = guidFactory.New(),
            UserName = username,
            PasswordHash = password
        };

        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();

        return user.Id;
    }
}