using MediatR;
using Microsoft.AspNetCore.Mvc;
using TgPoster.API.Common;
using TgPoster.Domain.UseCases.Files;

namespace TgPoster.API.Controllers;

//[Authorize]
[ApiController]
public class FileController(ISender sender) : ControllerBase
{
    [HttpGet(Routes.File.GetById)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Get(Guid fileId, CancellationToken cancellationToken)
    {
        var response = await sender.Send(new GetFileCommand(fileId), cancellationToken);
        return File(response.Data, response.ContentType);
    }
}