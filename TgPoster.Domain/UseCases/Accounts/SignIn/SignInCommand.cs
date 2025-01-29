using MediatR;

namespace TgPoster.Domain.UseCases.Accounts.SignIn;

public sealed record class SignInCommand(string Login, string Password) : IRequest<SignInResponse>;