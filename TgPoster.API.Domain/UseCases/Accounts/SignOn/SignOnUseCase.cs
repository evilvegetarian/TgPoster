using MediatR;
using Security.Interfaces;
using TgPoster.API.Domain.Exceptions;

namespace TgPoster.API.Domain.UseCases.Accounts.SignOn;

internal sealed class SignOnUseCase(IPasswordHasher passwordHasher, ISignOnStorage storage)
    : IRequestHandler<SignOnCommand, SignOnResponse>
{
    public async Task<SignOnResponse> Handle(SignOnCommand command, CancellationToken ct = default)
    {
        var passwordHash = passwordHasher.Generate(command.Password);

        if (await storage.HaveUserNameAsync(command.Login, ct))
        {
            throw new UserAlredyHasException();
        }

        var userId = await storage.CreateUserAsync(command.Login, passwordHash, ct);
        return new SignOnResponse
        {
            UserId = userId
        };
    }
}