namespace TgPoster.API.Domain.UseCases.Accounts.SignOn;

public interface ISignOnStorage
{
    Task<Guid> CreateUserAsync(string username, string password, CancellationToken token = default);
    Task<bool> HaveUserNameAsync(string userName, CancellationToken token = default);
}