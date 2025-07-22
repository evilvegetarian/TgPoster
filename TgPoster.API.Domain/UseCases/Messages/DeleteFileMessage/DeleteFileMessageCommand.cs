using MediatR;

namespace TgPoster.API.Domain.UseCases.Messages.DeleteFileMessage;

public sealed record DeleteFileMessageCommand(
    Guid Id,
    Guid FileId
) : IRequest;
