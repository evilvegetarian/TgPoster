namespace TgPoster.Domain.UseCases.SignOn;

public  interface ISignOnStorage
{
    Task<Guid> CreateUserAsync(string username, string password);
}