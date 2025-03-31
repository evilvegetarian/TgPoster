using MediatR;

namespace TgPoster.API.Domain.UseCases.Accounts.SignOn;

public sealed record SignOnCommand(string Login, string Password) : IRequest<SignOnResponse>;