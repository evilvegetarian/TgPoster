using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using TgPoster.API.Common;
using TgPoster.API.Models;
using TgPoster.Domain.UseCases.Messages.CreateMessagesFromFiles;
using TgPoster.Domain.UseCases.Messages.ListMessage;

namespace TgPoster.API.Controllers;

//[Authorize]
[ApiController]
public class MessageController(ISender sender) : ControllerBase
{
    [HttpPost(Routes.Message.CreateMessagesFromFiles)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> CreateMessagesFromFiles(
        CreateMessagesFromFilesRequest request,
        CancellationToken cancellationToken
    )
    {
        await sender.Send(new CreateMessagesFromFilesCommand(request.ScheduleId, request.Files), cancellationToken);
        return Created();
    }

    [HttpGet(Routes.Message.List)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<MessageResponse>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> List([FromQuery] [Required] Guid scheduleId, CancellationToken cancellationToken)
    {
        var response = await sender.Send(new ListMessageQuery(scheduleId), cancellationToken);
        return Ok(response);
    }
}