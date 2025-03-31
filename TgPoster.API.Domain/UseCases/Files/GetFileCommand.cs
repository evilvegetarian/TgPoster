using MediatR;

namespace TgPoster.API.Domain.UseCases.Files;

public record GetFileCommand(Guid FileId) : IRequest<GetFileResponse>;