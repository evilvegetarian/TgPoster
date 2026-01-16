using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TgPoster.API.Common;
using TgPoster.API.Domain.UseCases.Messages.ListMessage;
using TgPoster.API.Domain.UseCases.PromptSetting.CreatePromptSetting;
using TgPoster.API.Domain.UseCases.PromptSetting.EditPromptSetting;
using TgPoster.API.Domain.UseCases.PromptSetting.GetPromptSetting;
using TgPoster.API.Domain.UseCases.PromptSetting.ListPromptSetting;
using TgPoster.API.Models;

namespace TgPoster.API.Controllers;

/// <summary>
///     Контроллер управления промптов
/// </summary>
/// <param name="sender"></param>
[Authorize]
[ApiController]
public class PromptSettingController(ISender sender) : ControllerBase
{
	/// <summary>
	///     Создание промптов для расписания
	/// </summary>
	/// <param name="request">Данные для создания промптов (идентификатор расписания, текстовый, видео и фото промпты)</param>
	/// <param name="ctx">Токен отмены операции</param>
	/// <returns>Ответ с данными созданных промптов</returns>
	[HttpPost(Routes.PromptSetting.Create)]
	[ProducesResponseType(StatusCodes.Status201Created)]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> CreatePromptSetting(
		[FromBody] [Required] CreatePromptSettingRequest request,
		CancellationToken ctx
	)
	{
		var command = new CreatePromptSettingCommand(request.ScheduleId, request.TextPrompt, request.VideoPrompt,
			request.PhotoPrompt);
		var response = await sender.Send(command, ctx);
		return Created(Routes.PromptSetting.Create, response);
	}

	/// <summary>
	///     Получение промпта по id
	/// </summary>
	/// <param name="id">Идентификатор промпта</param>
	/// <param name="ctx">Токен отмены операции</param>
	/// <returns>Данные промпта с указанным идентификатором</returns>
	[HttpGet(Routes.PromptSetting.Get)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PromptSettingResponse))]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> GetPromptSetting([FromRoute] [Required] Guid id, CancellationToken ctx)
	{
		var query = new GetPromptSettingQuery(id);
		var response = await sender.Send(query, ctx);
		return Ok(response);
	}

	/// <summary>
	///     Получение списка промтов
	/// </summary>
	/// <param name="request">Параметры пагинации (номер страницы и размер страницы)</param>
	/// <param name="ctx">Токен отмены операции</param>
	/// <returns>Пагинированный список промптов</returns>
	[HttpGet(Routes.PromptSetting.List)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedResponse<PromptSettingResponse>))]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> ListPromptSetting(
		[FromQuery] ListPromptSettingRequest request,
		CancellationToken ctx
	)
	{
		var query = new ListPromptSettingQuery(request.PageNumber, request.PageSize);
		var response = await sender.Send(query, ctx);
		return Ok(response);
	}

	/// <summary>
	///     Изменение промптов для расписания
	/// </summary>
	/// <param name="id">Идентификатор промпта для обновления</param>
	/// <param name="request">Данные для обновления промптов (текстовый, видео и фото промпты)</param>
	/// <param name="ctx">Токен отмены операции</param>
	/// <returns>Результат выполнения операции</returns>
	[HttpPut(Routes.PromptSetting.Update)]
	[ProducesResponseType(StatusCodes.Status201Created)]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> EditPromptSetting(
		[FromRoute] [Required] Guid id,
		[FromBody] [Required] EditPromptSettingRequest request,
		CancellationToken ctx
	)
	{
		var command = new EditPromptSettingCommand(id, request.TextPrompt, request.VideoPrompt, request.PhotoPrompt);
		await sender.Send(command, ctx);
		return Ok();
	}
}