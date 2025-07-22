using MediatR;
using Microsoft.AspNetCore.Http;

namespace TgPoster.API.Domain.UseCases.Messages.LoadFilesMessage;

public sealed record LoadFilesMessageCommand(
    Guid Id,
    List<IFormFile> Files
) : IRequest;