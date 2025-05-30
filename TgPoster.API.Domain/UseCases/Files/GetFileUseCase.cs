using System.Globalization;
using MediatR;
using TgPoster.API.Domain.Exceptions;
using TgPoster.API.Domain.Services;

namespace TgPoster.API.Domain.UseCases.Files;

internal sealed class GetFileUseCase(FileService fileService) : IRequestHandler<GetFileCommand, GetFileResponse>
{
    public Task<GetFileResponse> Handle(GetFileCommand request, CancellationToken ct)
    {
        var file = fileService.RetrieveFileFromCache(request.FileId);
        if (file is null)
        {
            throw new FileNotExistException();
        }

        return Task.FromResult(new GetFileResponse(file.Data,
            file.ContentType,
            DateTime.UtcNow.ToString(CultureInfo.CurrentCulture)));
    }
}