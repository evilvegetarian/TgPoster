using Microsoft.EntityFrameworkCore;
using TgPoster.Domain.UseCases.SignOn;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.VO;

namespace TgPoster.Storage.Storages;

internal sealed class SignOnStorage(PosterContext context, IGuidFactory guidFactory) : ISignOnStorage
{
    public async Task<Guid> CreateUserAsync(string username, string password, CancellationToken token = default)
    {
        var user = new User
        {
            Id = guidFactory.New(),
            UserName = new UserName(username),
            PasswordHash = password
        };

        await context.Users.AddAsync(user, token);
        await context.SaveChangesAsync(token);

        return user.Id;
    }

    public async Task<bool> HaveUserNameAsync(string userName, CancellationToken token = default)
    {
        return await context.Users.AnyAsync(x =>
                x.UserName.Value.Equals(userName, StringComparison.CurrentCultureIgnoreCase),
            token);
    }
}