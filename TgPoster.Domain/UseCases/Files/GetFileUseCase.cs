using System.Globalization;
using MediatR;
using TgPoster.Domain.Services;

namespace TgPoster.Domain.UseCases.Files;

internal sealed class GetFileUseCase(FileService fileService) : IRequestHandler<GetFileCommand, FileResponse>
{
    public Task<FileResponse> Handle(GetFileCommand request, CancellationToken cancellationToken)
    {
        var file = fileService.RetrieveFileFromCache(request.FileId);
        if (file is null)
        {
            throw new FileNotFoundException();
        }

        return Task.FromResult(new FileResponse(file.Data,
            file.ContentType,
            DateTime.UtcNow.ToString(CultureInfo.CurrentCulture)));
    }
}