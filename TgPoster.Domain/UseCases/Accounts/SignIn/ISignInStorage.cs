using TgPoster.Domain.Models;

namespace TgPoster.Domain.UseCases.Accounts.SignIn;

public interface ISignInStorage
{
    Task<UserDto?> GetUserAsync(string login, CancellationToken token);
    Task CreateRefreshSession(Guid userId, Guid refreshToken, DateTimeOffset refreshDate, CancellationToken token);
}