using Auth;
using MediatR;

namespace TgPoster.Domain.UseCases.SignOn;

internal sealed class SignOnUseCase(IPasswordHasher passwordHasher, ISignOnStorage storage)
    : IRequestHandler<SignOnCommand, Guid>
{
    public async Task<Guid> Handle(SignOnCommand command, CancellationToken cancellationToken = default)
    {
        var passwordHash = passwordHasher.Generate(command.Password);

        if (await storage.HaveUserNameAsync(command.Login, cancellationToken))
        {
            throw new Exception("User already exists");
        }

        var userId = await storage.CreateUserAsync(command.Login, passwordHash, cancellationToken);
        return userId;
    }
}