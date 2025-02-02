using Microsoft.EntityFrameworkCore;
using TgPoster.Domain.Models;
using TgPoster.Domain.UseCases.Accounts.SignIn;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.VO;

namespace TgPoster.Storage.Storages;

internal class SignInStorage(PosterContext context) : ISignInStorage
{
    public async Task<UserDto?> GetUserAsync(string login, CancellationToken token)
    {
        return await context.Users.Where(x => x.UserName == new UserName(login))
            .Select(x => new UserDto
            {
                Id = x.Id,
                Username = x.UserName.Value,
                PasswordHash = x.PasswordHash
            })
            .FirstOrDefaultAsync(token);
    }

    public async Task CreateRefreshSession(
        Guid userId,
        Guid refreshToken,
        DateTimeOffset refreshDate,
        CancellationToken token
    )
    {
        var refresh = new RefreshSession
        {
            UserId = userId,
            RefreshToken = refreshToken,
            ExpiresAt = refreshDate
        };
        await context.RefreshSessions.AddAsync(refresh, token);
        await context.SaveChangesAsync(token);
    }
}