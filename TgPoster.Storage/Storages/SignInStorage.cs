using Microsoft.EntityFrameworkCore;
using TgPoster.Domain.Models;
using TgPoster.Domain.UseCases.SignIn;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.VO;

namespace TgPoster.Storage.Storages;

internal class SignInStorage(PosterContext context) : ISignInStorage
{
    public async Task<UserDto?> GetUserAsync(string login)
    {
        return await context.Users.Where(x => x.UserName == new UserName(login))
            .Select(x => new UserDto
            {
                Id = x.Id,
                Username = x.UserName.Value,
                PasswordHash = x.PasswordHash
            })
            .FirstOrDefaultAsync();
    }

    public async Task CreateRefreshSession(Guid userId, Guid refreshToken, DateTimeOffset refreshDate)
    {
        var refresh = new RefreshSession
        {
            UserId = userId,
            RefreshToken = refreshToken,
            ExpiresAt = refreshDate
        };
        await context.RefreshSessions.AddAsync(refresh);
        await context.SaveChangesAsync();
    }
}