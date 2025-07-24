using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TgPoster.API.Common;
using TgPoster.API.Domain.UseCases.Messages.ApproveMessages;
using TgPoster.API.Domain.UseCases.Messages.CreateMessage;
using TgPoster.API.Domain.UseCases.Messages.CreateMessagesFromFiles;
using TgPoster.API.Domain.UseCases.Messages.DeleteFileMessage;
using TgPoster.API.Domain.UseCases.Messages.EditMessage;
using TgPoster.API.Domain.UseCases.Messages.GetMessageById;
using TgPoster.API.Domain.UseCases.Messages.ListMessage;
using TgPoster.API.Domain.UseCases.Messages.LoadFilesMessage;
using TgPoster.API.Mapper;
using TgPoster.API.Models;

namespace TgPoster.API.Controllers;

/// <summary>
/// Контроллер для сообщений
/// </summary>
/// <param name="sender"></param>
[Authorize]
[ApiController]
public class MessageController(ISender sender) : ControllerBase
{
    /// <summary>
    ///     Создает сообщения из файлов, один файл = одно сообщение.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    [HttpPost(Routes.Message.CreateMessagesFromFiles)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> CreateMessagesFromFiles(
        [FromForm] CreateMessagesFromFilesRequest request,
        CancellationToken ct
    )
    {
        await sender.Send(new CreateMessagesFromFilesCommand(request.ScheduleId, request.Files), ct);
        return Created();
    }

    /// <summary>
    ///     Получение списка сообщений c пагинацией.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Пагинированный список сообщений.</returns>
    [HttpGet(Routes.Message.List)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedResponse<MessageResponse>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> List([FromQuery] ListMessagesRequest request, CancellationToken ct)
    {
        var query = request.ToDomain();
        var response = await sender.Send(query, ct);
        return Ok(response);
    }

    /// <summary>
    ///     Получение сообщения по Id
    /// </summary>
    /// <param name="id"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    [HttpGet(Routes.Message.Get)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MessageResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> Get([Required] Guid id, CancellationToken ct)
    {
        var response = await sender.Send(new GetMessageQuery(id), ct);
        return Ok(response);
    }

    /// <summary>
    ///     Создание сообщения
    /// </summary>
    /// <param name="request"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    [HttpPost(Routes.Message.Create)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(CreateMessageResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> Create([FromForm] CreateMessageRequest request, CancellationToken ct)
    {
        var response = await sender.Send(new CreateMessageCommand(
                request.ScheduleId, request.TimePosting, request.TextMessage, request.Files),
            ct);
        return Created(Routes.Message.Create, response);
    }

    /// <summary>
    ///     Изменения сообщения
    /// </summary>
    /// <param name="id"></param>
    /// <param name="request"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    [HttpPut(Routes.Message.Update)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> Update(
        [Required] Guid id,
        [FromForm] EditMessageRequest request,
        CancellationToken ct
    )
    {
        await sender.Send(new EditMessageCommand(
                id, request.ScheduleId, request.TimePosting, request.TextMessage, request.OldFiles, request.NewFiles),
            ct);
        return Ok();
    }

    /// <summary>
    ///     Удалить файл сообщения
    /// </summary>
    /// <param name="id"></param>
    /// <param name="fileId"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    [HttpDelete(Routes.Message.DeleteFileMessage)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> DeleteFileMessage(
        [Required] Guid id,
        [Required] Guid fileId,
        CancellationToken ct
    )
    {
        await sender.Send(new DeleteFileMessageCommand(id, fileId), ct);
        return Ok();
    }

    /// <summary>
    ///     Загрузить файлы сообщения
    /// </summary>
    /// <param name="id"></param>
    /// <param name="fileId"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    [HttpPatch(Routes.Message.LoadFiles)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> LoadFiles(
        [Required] Guid id,
        List<IFormFile> files,
        CancellationToken ct
    )
    {
        await sender.Send(new LoadFilesMessageCommand(id, files), ct);
        return Ok();
    }

    /// <summary>
    ///     Подтверждение сообщений
    /// </summary>
    /// <param name="request"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    [HttpPatch(Routes.Message.ApproveMessages)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> ApproveMessages(
        ApproveMessagesRequest request,
        CancellationToken ct
    )
    {
        await sender.Send(new ApproveMessagesCommand(request.MessagesIds), ct);
        return Ok();
    }
}