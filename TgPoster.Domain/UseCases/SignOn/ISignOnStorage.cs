namespace TgPoster.Domain.UseCases.SignOn;

public interface ISignOnStorage
{
    Task<Guid> CreateUserAsync(string username, string password, CancellationToken token = default);
    Task<bool> HaveUserNameAsync(string userName, CancellationToken token = default);
}