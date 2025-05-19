using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TgPoster.API.Common;
using TgPoster.API.Domain.UseCases.Files;

namespace TgPoster.API.Controllers;

[Authorize]
[ApiController]
public class FileController(ISender sender) : ControllerBase
{
    /// <summary>
    ///     Получение файла по Id
    /// </summary>
    /// <param name="id"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    [HttpGet(Routes.File.GetById)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileContentResult))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var response = await sender.Send(new GetFileCommand(id), ct);
        return File(response.Data, response.ContentType);
    }
}