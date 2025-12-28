using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TgPoster.API.Common;
using TgPoster.API.Domain.UseCases.OpenRouterSetting.AddScheduleOpenRouterSetting;
using TgPoster.API.Domain.UseCases.OpenRouterSetting.CreateOpenRouterSetting;
using TgPoster.API.Domain.UseCases.OpenRouterSetting.DeleteOpenRouterSetting;
using TgPoster.API.Domain.UseCases.OpenRouterSetting.GetOpenRouterSetting;
using TgPoster.API.Domain.UseCases.OpenRouterSetting.ListOpenRouterSetting;
using TgPoster.API.Models;

namespace TgPoster.API.Controllers;

/// <summary>
///     Контроллер Open Router
/// </summary>
/// <param name="sender"></param>
[Authorize]
[ApiController]
public class OpenRouterSettingController(ISender sender) : ControllerBase
{
	/// <summary>
	///     Создание настроек OpenRouter
	/// </summary>
	/// <param name="request"></param>
	/// <param name="ctx"></param>
	/// <returns>null</returns>
	[HttpPost(Routes.OpenRouterSetting.Create)]
	[ProducesResponseType(StatusCodes.Status201Created, Type = typeof(CreateOpenRouterSettingResponse))]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> CreateOpenRouterSetting(
		[FromBody] [Required] CreateOpenRouterSettingRequest request,
		CancellationToken ctx
	)
	{
		var command = new CreateOpenRouterSettingCommand(request.Token, request.Model);
		var response = await sender.Send(command, ctx);
		return Created(Routes.OpenRouterSetting.Create, response);
	}

	/// <summary>
	///     Получение настроек OpenRouter
	/// </summary>
	/// <param name="id"></param>
	/// <param name="ctx"></param>
	/// <returns>null</returns>
	[HttpGet(Routes.OpenRouterSetting.Get)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(GetOpenRouterSettingResponse))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> GetOpenRouterSetting([FromRoute] [Required] Guid id, CancellationToken ctx)
	{
		var command = new GetOpenRouterSettingQuery(id);
		var response = await sender.Send(command, ctx);
		return Ok(response);
	}


	/// <summary>
	///     Получение настроек OpenRouter
	/// </summary>
	/// <param name="ctx"></param>
	/// <returns>null</returns>
	[HttpGet(Routes.OpenRouterSetting.List)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ListOpenRouterSettingResponse))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> GetOpenRouterSetting(CancellationToken ctx)
	{
		var command = new ListOpenRouterSettingQuery();
		var response = await sender.Send(command, ctx);
		return Ok(response);
	}

	/// <summary>
	///     Получение настроек OpenRouter
	/// </summary>
	/// <param name="id"></param>
	/// <param name="ctx"></param>
	/// <returns>null</returns>
	[HttpDelete(Routes.OpenRouterSetting.Delete)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ListOpenRouterSettingResponse))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> Delete([FromRoute] [Required] Guid id, CancellationToken ctx)
	{
		var command = new DeleteOpenRouterSettingCommand(id);
		await sender.Send(command, ctx);
		return Ok();
	}

	/// <summary>
	///     Добавление расписания к настройкам OpenRouter
	/// </summary>
	/// <param name="id"></param>
	/// <param name="scheduleId"></param>
	/// <param name="ctx"></param>
	/// <returns>null</returns>
	[HttpPatch(Routes.OpenRouterSetting.AddSchedule)]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> AddSchedule(
		[FromRoute] [Required] Guid id,
		[FromRoute] [Required] Guid scheduleId,
		CancellationToken ctx
	)
	{
		var command = new AddScheduleOpenRouterSettingCommand(id, scheduleId);
		await sender.Send(command, ctx);
		return Ok();
	}
}