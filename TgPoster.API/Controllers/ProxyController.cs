using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TgPoster.API.Common;
using TgPoster.API.Domain.UseCases.Proxies.CreateProxy;
using TgPoster.API.Domain.UseCases.Proxies.DeleteProxy;
using TgPoster.API.Domain.UseCases.Proxies.ListProxies;
using TgPoster.API.Domain.UseCases.Proxies.UpdateProxy;
using TgPoster.API.Models;

namespace TgPoster.API.Controllers;

/// <summary>
///     Управление прокси для Telegram сессий
/// </summary>
[Authorize]
[ApiController]
public class ProxyController(ISender sender) : ControllerBase
{
	/// <summary>
	///     Создание прокси
	/// </summary>
	[HttpPost(Routes.Proxy.Create)]
	[ProducesResponseType(StatusCodes.Status201Created, Type = typeof(CreateProxyResponse))]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> Create(
		[FromBody] [Required] CreateProxyRequest request,
		CancellationToken ct
	)
	{
		var response = await sender.Send(new CreateProxyCommand(
			request.Name,
			request.Type,
			request.Host,
			request.Port,
			request.Username,
			request.Password,
			request.Secret), ct);
		return Created(Routes.Proxy.Create, response);
	}

	/// <summary>
	///     Список прокси текущего пользователя
	/// </summary>
	[HttpGet(Routes.Proxy.List)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProxyListResponse))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> List(CancellationToken ct)
	{
		var response = await sender.Send(new ListProxiesQuery(), ct);
		return Ok(response);
	}

	/// <summary>
	///     Обновление прокси
	/// </summary>
	[HttpPut(Routes.Proxy.Update)]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> Update(
		[FromRoute] [Required] Guid id,
		[FromBody] [Required] UpdateProxyRequest request,
		CancellationToken ct
	)
	{
		await sender.Send(new UpdateProxyCommand(
			id,
			request.Name,
			request.Type,
			request.Host,
			request.Port,
			request.Username,
			request.Password,
			request.Secret), ct);
		return NoContent();
	}

	/// <summary>
	///     Удаление прокси (привязанные сессии остаются без прокси)
	/// </summary>
	[HttpDelete(Routes.Proxy.Delete)]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> Delete([FromRoute] [Required] Guid id, CancellationToken ct)
	{
		await sender.Send(new DeleteProxyCommand(id), ct);
		return NoContent();
	}
}
