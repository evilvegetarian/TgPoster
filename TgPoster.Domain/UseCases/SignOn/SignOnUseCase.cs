using Auth;
using MediatR;

namespace TgPoster.Domain.UseCases.SignOn;

public sealed class SignOnUseCase(IPasswordHasher passwordHasher, ISignOnStorage storage): IRequestHandler<SignOnCommand>
{
    public async Task Handle(SignOnCommand command,CancellationToken cancellationToken = default)
    {
        var passwordHash = passwordHasher.Generate(command.Password);
        await storage.CreateUserAsync(command.Login, passwordHash);
    }
}