using MediatR;
using Security.Interfaces;

namespace TgPoster.API.Domain.UseCases.Accounts.SignOn;

internal sealed class SignOnUseCase(IPasswordHasher passwordHasher, ISignOnStorage storage)
    : IRequestHandler<SignOnCommand, SignOnResponse>
{
    public async Task<SignOnResponse> Handle(SignOnCommand command, CancellationToken ct = default)
    {
        var passwordHash = passwordHasher.Generate(command.Password);

        if (await storage.HaveUserNameAsync(command.Login, ct))
        {
            throw new Exception("User already exists");
        }

        var userId = await storage.CreateUserAsync(command.Login, passwordHash, ct);
        return new SignOnResponse
        {
            UserId = userId
        };
    }
}