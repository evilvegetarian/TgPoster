using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using TgPoster.API.Common;
using TgPoster.API.Domain.UseCases.Messages.CreateMessage;
using TgPoster.API.Domain.UseCases.Messages.CreateMessagesFromFiles;
using TgPoster.API.Domain.UseCases.Messages.GetMessageById;
using TgPoster.API.Domain.UseCases.Messages.ListMessage;
using TgPoster.API.Models;

namespace TgPoster.API.Controllers;

//[Authorize]
[ApiController]
public class MessageController(ISender sender) : ControllerBase
{
    /// <summary>
    ///     Создает сообщения из файлов, один файл = одно сообщение.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
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

    /// <summary>
    ///     Получение списка сообщений
    /// </summary>
    /// <param name="scheduleId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet(Routes.Message.List)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<MessageResponse>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> List([FromQuery] [Required] Guid scheduleId, CancellationToken cancellationToken)
    {
        var response = await sender.Send(new ListMessageQuery(scheduleId), cancellationToken);
        return Ok(response);
    }

    /// <summary>
    ///     Получение сообщения по Id
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet(Routes.Message.GetById)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MessageResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> Get([Required] Guid id, CancellationToken cancellationToken)
    {
        var response = await sender.Send(new GetMessageQuery(id), cancellationToken);
        return Ok(response);
    }

    /// <summary>
    ///     Создание сообщения
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost(Routes.Message.Create)]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(CreateMessageResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> Create(CreateMessageRequest request, CancellationToken cancellationToken)
    {
        var response = await sender.Send(
            new CreateMessageCommand(request.ScheduleId, request.TimePosting, request.TextMessage, request.Files),
            cancellationToken);
        return Created(Routes.Message.Create, response);
    }
}