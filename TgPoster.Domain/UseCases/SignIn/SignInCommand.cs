using MediatR;

namespace TgPoster.Domain.UseCases.SignIn;

public sealed record class SignInCommand(string Login, string Password) : IRequest<SignInResponse>;