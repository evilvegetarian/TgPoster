using MediatR;

namespace TgPoster.Domain.UseCases.Accounts.SignOn;

public sealed record SignOnCommand(string Login, string Password) : IRequest<SignOnResponse>;