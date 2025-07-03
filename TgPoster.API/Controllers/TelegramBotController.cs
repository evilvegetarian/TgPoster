using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TgPoster.API.Common;
using TgPoster.API.Domain.UseCases.TelegramBots.CreateTelegramBot;
using TgPoster.API.Domain.UseCases.TelegramBots.DeleteTelegramBot;
using TgPoster.API.Domain.UseCases.TelegramBots.ListTelegramBot;
using TgPoster.API.Models;

namespace TgPoster.API.Controllers;

[Authorize]
[ApiController]
public class TelegramBotController(ISender sender) : ControllerBase
{
    /// <summary>
    ///     Добавление бота
    /// </summary>
    /// <param name="botRequest"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    [HttpPost(Routes.TelegramBot.Create)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CreateTelegramBotResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> Create(CreateTelegramBotRequest botRequest, CancellationToken ct)
    {
        var response = await sender.Send(new CreateTelegramBotCommand(botRequest.Token), ct);
        return Ok(response);
    }

    /// <summary>
    ///     Получение ботов
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    [HttpGet(Routes.TelegramBot.List)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<TelegramBotResponse>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var response = await sender.Send(new ListTelegramBotQuery(), ct);
        return Ok(response);
    }

    /// <summary>
    ///     Удаление бота
    /// </summary>
    /// <param name="id"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    [HttpDelete(Routes.TelegramBot.Delete)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await sender.Send(new DeleteTelegramCommand(id), ct);
        return Ok();
    }
}