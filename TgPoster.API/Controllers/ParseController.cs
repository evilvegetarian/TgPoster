using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TgPoster.API.Common;
using TgPoster.API.Domain.UseCases.Parse.ListChannel;
using TgPoster.API.Domain.UseCases.Schedules.ListSchedule;
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

    /// <summary>
    /// Список парсингов каналов
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    [HttpGet(Routes.Parse.List)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ParseChannelsResponse>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var list = await sender.Send(new ListParseChannelsQuery(), ct);
        return Ok(list);
    }
}