using MediatR;

namespace TgPoster.API.Domain.UseCases.Accounts.SignIn;

public sealed record SignInCommand(string Login, string Password) : IRequest<SignInResponse>;