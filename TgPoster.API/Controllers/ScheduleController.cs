using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TgPoster.API.Common;
using TgPoster.API.Domain.UseCases.Schedules.CreateSchedule;
using TgPoster.API.Domain.UseCases.Schedules.DeleteSchedule;
using TgPoster.API.Domain.UseCases.Schedules.GetSchedule;
using TgPoster.API.Domain.UseCases.Schedules.ListSchedule;
using TgPoster.API.Domain.UseCases.Schedules.UpdateActiveSchedule;
using TgPoster.API.Domain.UseCases.Schedules.UpdateSchedule;
using TgPoster.API.Models;

namespace TgPoster.API.Controllers;

/// <summary>
///     Контроллер для управления расписаниями
/// </summary>
[Authorize]
[ApiController]
public class ScheduleController(ISender sender) : ControllerBase
{
	/// <summary>
	///     Получение расписаний
	/// </summary>
	/// <param name="ct">Токен отмены операции</param>
	/// <returns>Список всех расписаний</returns>
	[HttpGet(Routes.Schedule.List)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ScheduleResponse>))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> List(CancellationToken ct)
	{
		var response = await sender.Send(new ListScheduleQuery(), ct);
		return Ok(response);
	}

	/// <summary>
	///     Создание расписания
	/// </summary>
	/// <param name="request">Данные для создания расписания</param>
	/// <param name="ct">Токен отмены операции</param>
	/// <returns>Созданное расписание с идентификатором</returns>
	[HttpPost(Routes.Schedule.Create)]
	[ProducesResponseType(StatusCodes.Status201Created, Type = typeof(CreateScheduleResponse))]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> Create([FromBody] [Required] CreateScheduleRequest request, CancellationToken ct)
	{
		var response =
			await sender.Send(
				new CreateScheduleCommand(request.Name, request.TelegramBotId, request.Channel,
					request.YouTubeAccountId), ct);
		return Created(Routes.Schedule.Create, response);
	}

	/// <summary>
	///     Получение расписания
	/// </summary>
	/// <param name="id">Идентификатор расписания</param>
	/// <param name="ct">Токен отмены операции</param>
	/// <returns>Информация о расписании</returns>
	[HttpGet(Routes.Schedule.Get)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ScheduleResponse))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> Get([FromRoute] [Required] Guid id, CancellationToken ct)
	{
		var response = await sender.Send(new GetScheduleCommand(id), ct);
		return Ok(response);
	}

	/// <summary>
	///     Удаление расписания
	/// </summary>
	/// <param name="id">Идентификатор расписания</param>
	/// <param name="ct">Токен отмены операции</param>
	/// <returns>Результат операции удаления</returns>
	[HttpDelete(Routes.Schedule.Delete)]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> Delete([FromRoute] [Required] Guid id, CancellationToken ct)
	{
		await sender.Send(new DeleteScheduleCommand(id), ct);
		return NoContent();
	}

	/// <summary>
	///     Изменить активность расписания
	/// </summary>
	/// <param name="id">Идентификатор расписания</param>
	/// <param name="ct">Токен отмены операции</param>
	/// <returns>Результат операции обновления статуса</returns>
	[HttpPatch(Routes.Schedule.UpdateStatus)]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> UpdateStatus([FromRoute] [Required] Guid id, CancellationToken ct)
	{
		await sender.Send(new UpdateStatusScheduleCommand(id), ct);
		return NoContent();
	}

	/// <summary>
	///     Обновить расписание
	/// </summary>
	/// <param name="id">Идентификатор расписания</param>
	/// <param name="request">Данные для обновления расписания</param>
	/// <param name="ct">Токен отмены операции</param>
	/// <returns>Результат операции обновления</returns>
	[HttpPut(Routes.Schedule.Update)]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> Update(
		[FromRoute] [Required] Guid id,
		[FromBody] [Required] UpdateScheduleRequest request,
		CancellationToken ct
	)
	{
		await sender.Send(new UpdateScheduleCommand(id, request.YouTubeAccountId), ct);
		return NoContent();
	}
}