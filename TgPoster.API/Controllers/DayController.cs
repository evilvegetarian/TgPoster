using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TgPoster.API.Common;
using TgPoster.API.Domain.UseCases.Days.CreateDays;
using TgPoster.API.Domain.UseCases.Days.GetDayOfWeek;
using TgPoster.API.Domain.UseCases.Days.GetDays;
using TgPoster.API.Domain.UseCases.Days.UpdateTimeDay;
using TgPoster.API.Models;

namespace TgPoster.API.Controllers;

[Authorize]
[ApiController]
public class DayController(ISender sender) : ControllerBase
{
    /// <summary>
    ///     Получение дней недели
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    [HttpGet(Routes.Day.DayOfWeek)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<DayOfWeekResponse>))]
    public async Task<IActionResult> GetDayOfWeek(CancellationToken ct)
    {
        var response = await sender.Send(new DayOfWeekQuery(), ct);
        return Ok(response);
    }

    /// <summary>
    ///     Создание дней недели
    /// </summary>
    /// <param name="request"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    [HttpPost(Routes.Day.Create)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateDay(CreateDaysRequest request, CancellationToken ct)
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
        await sender.Send(command, ct);
        return Created();
    }

    /// <summary>
    ///     Получение дней
    /// </summary>
    /// <param name="scheduleId"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    [HttpGet(Routes.Day.Get)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<GetDaysResponse>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDays([FromQuery] [Required] Guid scheduleId, CancellationToken ct)
    {
        var response = await sender.Send(new GetDaysQuery(scheduleId), ct);
        return Ok(response);
    }

    /// <summary>
    ///     Обновление времени для определенного дня
    /// </summary>
    /// <param name="request"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    [HttpPatch(Routes.Day.UpdateTime)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateTime(UpdateTimeRequest request, CancellationToken ct)
    {
        await sender.Send(new UpdateTimeCommand(request.Id, request.Times), ct);
        return Ok();
    }
}