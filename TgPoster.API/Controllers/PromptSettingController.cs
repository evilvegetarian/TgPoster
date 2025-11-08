using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TgPoster.API.Common;
using TgPoster.API.Domain.UseCases.PromptSetting.CreatePromptSetting;
using TgPoster.API.Models;

namespace TgPoster.API.Controllers;

/// <summary>
/// Контроллер создания промптов
/// </summary>
/// <param name="sender"></param>
[Authorize]
[ApiController]
public class PromptSettingController(ISender sender) : ControllerBase
{
	/// <summary>
	/// Создание промптов для расписания
	/// </summary>
	/// <param name="request"></param>
	/// <param name="ctx"></param>
	/// <returns></returns>
	[HttpPost(Routes.PromptSetting.Create)]
	[ProducesResponseType(StatusCodes.Status201Created)]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> CreatePromptSetting(CreatePromptSettingRequest request, CancellationToken ctx)
	{
		var command = new CreatePromptSettingCommand(request.ScheduleId, request.TextPrompt, request.VideoPrompt, request.PhotoPrompt);
		await sender.Send(command, ctx);
		return Created();
	}
}