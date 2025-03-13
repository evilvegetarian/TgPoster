using MediatR;
using Microsoft.AspNetCore.Mvc;
using TgPoster.API.Common;
using TgPoster.Domain.UseCases.Files;
using TgPoster.Domain.UseCases.Messages.ListMessage;

namespace TgPoster.API.Controllers;

//[Authorize]
[ApiController]
public class FileController(ISender sender) : ControllerBase
{
    /// <summary>
    ///     Получение файла по Id
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet(Routes.File.GetById)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileContentResult))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var response = await sender.Send(new GetFileCommand(id), cancellationToken);
        return File(response.Data, response.ContentType);
    }
}