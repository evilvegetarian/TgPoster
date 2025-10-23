namespace TgPoster.API.Domain.UseCases.Accounts.SignOn;

public interface ISignOnStorage
{
	Task<Guid> CreateUserAsync(string username, string password, CancellationToken token);
	Task<bool> HaveUserNameAsync(string userName, CancellationToken token);
}