using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TgPoster.API.Common;
using TgPoster.API.Domain.UseCases.OpenRouterSetting.CreateOpenRouterSetting;
using TgPoster.API.Domain.UseCases.OpenRouterSetting.GetOpenRouterSetting;
using TgPoster.API.Models;

namespace TgPoster.API.Controllers;

/// <summary>
/// Контроллер Open Router
/// </summary>
/// <param name="sender"></param>
[Authorize]
[ApiController]
public class OpenRouterSettingController(ISender sender) : ControllerBase
{
	/// <summary>
	/// Создание настроек OpenRouter
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
		CreateOpenRouterSettingRequest request,
		CancellationToken ctx
	)
	{
		var command = new CreateOpenRouterSettingCommand(request.Token, request.Model);
		var response = await sender.Send(command, ctx);
		return Created(Routes.OpenRouterSetting.Create, response);
	}

	/// <summary>
	/// Получение настроек OpenRouter
	/// </summary>
	/// <param name="id"></param>
	/// <param name="ctx"></param>
	/// <returns>null</returns>
	[HttpGet(Routes.OpenRouterSetting.Get)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(GetOpenRouterSettingResponse))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> GetOpenRouterSetting(Guid id, CancellationToken ctx)
	{
		var command = new GetOpenRouterSettingQuery(id);
		var response = await sender.Send(command, ctx);
		return Ok(response);
	}
}