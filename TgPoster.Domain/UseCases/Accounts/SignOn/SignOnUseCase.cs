using Security;
using MediatR;

namespace TgPoster.Domain.UseCases.Accounts.SignOn;

internal sealed class SignOnUseCase(IPasswordHasher passwordHasher, ISignOnStorage storage)
    : IRequestHandler<SignOnCommand, SignOnResponse>
{
    public async Task<SignOnResponse> Handle(SignOnCommand command, CancellationToken cancellationToken = default)
    {
        var passwordHash = passwordHasher.Generate(command.Password);

        if (await storage.HaveUserNameAsync(command.Login, cancellationToken))
        {
            throw new Exception("User already exists");
        }

        var userId = await storage.CreateUserAsync(command.Login, passwordHash, cancellationToken);
        return new SignOnResponse
        {
            UserId = userId,
        };
    }
}