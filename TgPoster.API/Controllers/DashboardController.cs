using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TgPoster.API.Common;
using TgPoster.API.Domain.UseCases.Dashboard.GetDashboard;

namespace TgPoster.API.Controllers;

/// <summary>
/// Контроллер главной страницы
/// </summary>
/// <param name="sender"></param>
[Authorize]
[ApiController]
public class DashboardController(ISender sender) : ControllerBase
{
	/// <summary>
	/// Получить данные главной страницы текущего пользователя
	/// </summary>
	/// <param name="ct"></param>
	/// <returns></returns>
	[HttpGet(Routes.Dashboard.Root)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DashboardResponse))]
	[ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> Get(CancellationToken ct)
	{
		var response = await sender.Send(new GetDashboardQuery(), ct);
		return Ok(response);
	}
}
