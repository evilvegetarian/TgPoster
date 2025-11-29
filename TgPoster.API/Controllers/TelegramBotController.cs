using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TgPoster.API.Common;
using TgPoster.API.Domain.UseCases.TelegramBots.CreateTelegramBot;
using TgPoster.API.Domain.UseCases.TelegramBots.DeleteTelegramBot;
using TgPoster.API.Domain.UseCases.TelegramBots.ListTelegramBot;
using TgPoster.API.Domain.UseCases.TelegramBots.UpdateTelegramBot;
using TgPoster.API.Models;

namespace TgPoster.API.Controllers;

/// <summary>
///     Управление ботами
/// </summary>
/// <param name="sender"></param>
[Authorize]
[ApiController]
public class TelegramBotController(ISender sender) : ControllerBase
{
	/// <summary>
	///     Добавление бота
	/// </summary>
	/// <param name="request"></param>
	/// <param name="ct">CancellationToken</param>
	/// <returns></returns>
	[HttpPost(Routes.TelegramBot.Create)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CreateTelegramBotResponse))]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> Create(
		[FromBody] [Required] CreateTelegramBotRequest request,
		CancellationToken ct
	)
	{
		var response = await sender.Send(new CreateTelegramBotCommand(request.Token), ct);
		return Ok(response);
	}

	/// <summary>
	///     Обновление бота
	/// </summary>
	/// <param name="id"></param>
	/// <param name="request"></param>
	/// <param name="ct">CancellationToken</param>
	/// <returns></returns>
	[HttpPut(Routes.TelegramBot.Update)]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> Update(
		[FromRoute] [Required] Guid id,
		[FromBody] [Required] UpdateTelegramBotRequest request,
		CancellationToken ct
	)
	{
		await sender.Send(new UpdateTelegramBotCommand(id, request.Name, request.IsActive), ct);
		return Ok();
	}

	/// <summary>
	///     Получение всех ботов пользователя
	/// </summary>
	/// <param name="ct">CancellationToken</param>
	/// <returns></returns>
	[HttpGet(Routes.TelegramBot.List)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<TelegramBotResponse>))]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> List(CancellationToken ct)
	{
		var response = await sender.Send(new ListTelegramBotQuery(), ct);
		return Ok(response);
	}

	/// <summary>
	///     Удаление бота
	/// </summary>
	/// <param name="id"></param>
	/// <param name="ct">CancellationToken</param>
	/// <returns></returns>
	[HttpDelete(Routes.TelegramBot.Delete)]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> Delete([FromRoute] [Required] Guid id, CancellationToken ct)
	{
		await sender.Send(new DeleteTelegramCommand(id), ct);
		return Ok();
	}
}