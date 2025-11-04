using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TgPoster.API.Common;
using TgPoster.API.Domain.UseCases.OpenRouterSetting.CreateOpenRouterSetting;
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
	/// <returns></returns>
	[HttpPost(Routes.OpenRouterSetting.Create)]
	public async Task<IActionResult> CreateOpenRouterSetting(CreateOpenRouterSettingRequest request, CancellationToken ctx)
	{
		var command = new CreateOpenRouterSettingCommand(request.Token, request.Model);
		await sender.Send(command, ctx);
		return Created();
	}
}