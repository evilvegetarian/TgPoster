using MediatR;

namespace TgPoster.API.Domain.UseCases.Accounts.SignIn;

public sealed record class SignInCommand(string Login, string Password) : IRequest<SignInResponse>;