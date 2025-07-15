using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TgPoster.API.Common;
using TgPoster.API.Mapper;
using TgPoster.API.Models;

namespace TgPoster.API.Controllers;

[Authorize]
[ApiController]
public class ParseController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Парсинг канала
    /// </summary>
    /// <param name="request"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    [HttpPost(Routes.Parse.ParseChannel)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> ParseChannel(ParseChannelRequest request, CancellationToken ct)
    {
        await sender.Send(request.ToCommand(), ct);
        return Created();
    }
}