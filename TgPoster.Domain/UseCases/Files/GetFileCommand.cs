using MediatR;

namespace TgPoster.Domain.UseCases.Files;

public record GetFileCommand(Guid FileId) : IRequest<GetFileResponse>;