using TgPoster.API.Domain.Models;

namespace TgPoster.API.Domain.UseCases.Accounts.SignIn;

public interface ISignInStorage
{
	Task<UserDto?> GetUserAsync(string login, CancellationToken token);
	Task CreateRefreshSessionAsync(Guid userId, Guid refreshToken, DateTimeOffset refreshDate, CancellationToken token);
}