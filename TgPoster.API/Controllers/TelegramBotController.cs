using MediatR;
using Microsoft.AspNetCore.Mvc;
using TgPoster.API.Common;
using TgPoster.API.Models;
using TgPoster.Domain.UseCases.TelegramBots.CreateTelegramBot;
using TgPoster.Domain.UseCases.TelegramBots.ListTelegramBot;

namespace TgPoster.API.Controllers;

//[Authorize]
[ApiController]
public class TelegramBotController(ISender sender) : ControllerBase
{
    /// <summary>
    ///     Добавление бота
    /// </summary>
    /// <param name="botRequest"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost(Routes.TelegramBot.Create)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CreateTelegramBotResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> Create(CreateTelegramBotRequest botRequest, CancellationToken cancellationToken)
    {
        var response = await sender.Send(new CreateTelegramBotCommand(botRequest.Token), cancellationToken);
        return Ok(response);
    }

    /// <summary>
    ///     Получение ботов
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet(Routes.TelegramBot.List)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<TelegramBotResponse>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var response = await sender.Send(new ListTelegramBotQuery(), cancellationToken);
        return Ok(response);
    }
}