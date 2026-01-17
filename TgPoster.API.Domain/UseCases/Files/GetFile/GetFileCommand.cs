using MediatR;

namespace TgPoster.API.Domain.UseCases.Files.GetFile;

public record GetFileCommand(Guid FileId) : IRequest<GetFileResponse>;