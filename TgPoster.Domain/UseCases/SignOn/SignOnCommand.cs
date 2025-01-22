using MediatR;

namespace TgPoster.Domain.UseCases.SignOn;

public sealed record SignOnCommand(string Login, string Password) : IRequest;
