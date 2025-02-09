using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using TgPoster.API.Common;
using TgPoster.API.Models;
using TgPoster.Domain.UseCases.Days.CreateDays;
using TgPoster.Domain.UseCases.Days.GetDayOfWeek;
using TgPoster.Domain.UseCases.Days.GetDays;
using TgPoster.Domain.UseCases.Days.UpdateTimeDay;

namespace TgPoster.API.Controllers;

[ApiController]
public class DayController(ISender sender) : ControllerBase
{
    /// <summary>
    ///     Получение дней недели
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet(Routes.Day.DayOfWeek)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<DayOfWeekResponse>))]
    public async Task<IActionResult> GetDayOfWeek(CancellationToken cancellationToken)
    {
        var response = await sender.Send(new DayOfWeekQuery(), cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost(Routes.Day.Create)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateDay(CreateDaysRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateDaysCommand(
            request.ScheduleId,
            request.DaysOfWeek
                .Select(x => new DayOfWeekForm(
                    x.DayOfWeekPosting,
                    x.StartPosting,
                    x.EndPosting,
                    x.Interval)
                ).ToList()
        );
        await sender.Send(command, cancellationToken);
        return Ok();
    }

    /// <summary>
    ///     Получение дней
    /// </summary>
    /// <param name="scheduleId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet(Routes.Day.Get)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<GetDaysResponse>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDays([FromQuery] [Required] Guid scheduleId, CancellationToken cancellationToken)
    {
        var response = await sender.Send(new GetDaysQuery(scheduleId), cancellationToken);
        return Ok(response);
    }

    /// <summary>
    ///     Обновление времени для определенного дня
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPatch(Routes.Day.UpdateTime)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateTime(UpdateTimeRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new UpdateTimeCommand(request.Id, request.Times), cancellationToken);
        return Ok();
    }
}