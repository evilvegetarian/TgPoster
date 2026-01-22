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

/// <summary>
///     Контроллер управления дней
/// </summary>
/// <param name="sender"></param>
[Authorize]
[ApiController]
public class DayController(ISender sender) : ControllerBase
{
	/// <summary>
	///     Получение дней недели
	/// </summary>
	/// <param name="ct">Токен отмены операции</param>
	/// <returns>Список всех дней недели для выбора</returns>
	[HttpGet(Routes.Day.DayOfWeek)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<DayOfWeekResponse>))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> GetDayOfWeek(CancellationToken ct)
	{
		var response = await sender.Send(new DayOfWeekQuery(), ct);
		return Ok(response);
	}

	/// <summary>
	///     Создание дней недели
	/// </summary>
	/// <param name="request">
	///     Данные для создания дней недели с временем публикации (расписание, дни недели, время начала,
	///     окончания и интервал)
	/// </param>
	/// <param name="ct">Токен отмены операции</param>
	/// <returns>Результат выполнения операции</returns>
	[HttpPost(Routes.Day.Create)]
	[ProducesResponseType(StatusCodes.Status201Created)]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> Create(CreateDaysRequest request, CancellationToken ct)
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
	/// <param name="scheduleId">Идентификатор расписания</param>
	/// <param name="ct">Токен отмены операции</param>
	/// <returns>Список дней недели с настройками публикации для указанного расписания</returns>
	[HttpGet(Routes.Day.GetBySchedule)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<GetDaysResponse>))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> Get([FromQuery] [Required] Guid scheduleId, CancellationToken ct)
	{
		var response = await sender.Send(new GetDaysQuery(scheduleId), ct);
		return Ok(response);
	}

	/// <summary>
	///     Обновление времени для определенного дня
	/// </summary>
	/// <param name="request">Данные для обновления времени (идентификатор расписания, день недели и новое время)</param>
	/// <param name="ct">Токен отмены операции</param>
	/// <returns>Результат выполнения операции</returns>
	[HttpPatch(Routes.Day.UpdateTime)]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> UpdateTime(UpdateTimeRequest request, CancellationToken ct)
	{
		await sender.Send(new UpdateTimeCommand(request.ScheduleId, request.DayOfWeek, request.Times), ct);
		return Ok();
	}
}