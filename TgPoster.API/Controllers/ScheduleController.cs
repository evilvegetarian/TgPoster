using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TgPoster.API.Common;
using TgPoster.API.Models;
using TgPoster.Domain.UseCases.Schedules.CreateSchedule;
using TgPoster.Domain.UseCases.Schedules.DeleteSchedule;
using TgPoster.Domain.UseCases.Schedules.GetSchedule;
using TgPoster.Domain.UseCases.Schedules.ListSchedule;

namespace TgPoster.API.Controllers;

//[Authorize]
[ApiController]
public class ScheduleController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Получение расписаний
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet(Routes.Schedule.List)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ScheduleResponse>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var response = await sender.Send(new ListScheduleQuery(), cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Создание расписания
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost(Routes.Schedule.Create)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CreateScheduleResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> Create([FromBody] CreateScheduleRequest request, CancellationToken cancellationToken)
    {
        var response = await sender.Send(new CreateScheduleCommand(request.Name), cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Получение расписания 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet(Routes.Schedule.GetById)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ScheduleResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var response = await sender.Send(new GetScheduleCommand(id), cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Удаление расписания
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpDelete(Routes.Schedule.Delete)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteScheduleCommand(id), cancellationToken);
        return Ok();
    }
}