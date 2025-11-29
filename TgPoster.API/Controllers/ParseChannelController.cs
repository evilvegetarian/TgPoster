using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TgPoster.API.Common;
using TgPoster.API.Domain.UseCases.Parse.CreateParseChannel;
using TgPoster.API.Domain.UseCases.Parse.DeleteParseChannel;
using TgPoster.API.Domain.UseCases.Parse.ListParseChannel;
using TgPoster.API.Mapper;
using TgPoster.API.Models;

namespace TgPoster.API.Controllers;

/// <summary>
/// </summary>
/// <param name="sender"></param>
[Authorize]
[ApiController]
public class ParseChannelController(ISender sender) : ControllerBase
{
	/// <summary>
	///     Создания задания для парсинга канала
	/// </summary>
	/// <param name="request"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	[HttpPost(Routes.ParseChannel.Create)]
	[ProducesResponseType(StatusCodes.Status201Created, Type = typeof(CreateParseChannelResponse))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> Create(
		[FromBody] [Required] CreateParseChannelRequest request,
		CancellationToken ct
	)
	{
		var response = await sender.Send(request.ToCommand(), ct);
		return Created(Routes.ParseChannel.List, response);
	}

	/// <summary>
	///     Все задачи парсинга
	/// </summary>
	/// <param name="ct"></param>
	/// <returns></returns>
	[HttpGet(Routes.ParseChannel.List)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ParseChannelsResponse>))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> List(CancellationToken ct)
	{
		var list = await sender.Send(new ListParseChannelsQuery(), ct);
		return Ok(list);
	}

	/// <summary>
	///     Изменения задачи на парсинг
	/// </summary>
	/// <param name="request"></param>
	/// <param name="ct"></param>
	/// <param name="id"></param>
	/// <returns></returns>
	[HttpPut(Routes.ParseChannel.Update)]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> Update(
		[FromRoute] [Required] Guid id,
		[FromBody] [Required] UpdateParseChannelRequest request,
		CancellationToken ct
	)
	{
		await sender.Send(request.ToCommand(id), ct);
		return Ok();
	}

	/// <summary>
	///     Удаления задачи на парсинг
	/// </summary>
	/// <param name="ct"></param>
	/// <param name="id"></param>
	/// <returns></returns>
	[HttpDelete(Routes.ParseChannel.Delete)]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> Delete([FromRoute] [Required] Guid id, CancellationToken ct)
	{
		await sender.Send(new DeleteParseChannelCommand(id), ct);
		return Ok();
	}
}