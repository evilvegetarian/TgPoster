using TgPoster.Domain.Models;

namespace TgPoster.Domain.UseCases.SignIn;

public interface ISignInStorage
{
    Task<UserDto?> GetUserAsync(string login);
    Task CreateRefreshSession(Guid userId, Guid refreshToken, DateTimeOffset refreshDate);
}