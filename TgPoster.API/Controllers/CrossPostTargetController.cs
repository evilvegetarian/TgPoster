using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TgPoster.API.Common;
using TgPoster.API.Domain.UseCases.CrossPostTargets.CreateCrossPostTarget;
using TgPoster.API.Domain.UseCases.CrossPostTargets.DeleteCrossPostTarget;
using TgPoster.API.Domain.UseCases.CrossPostTargets.ListCrossPostTargets;
using TgPoster.API.Domain.UseCases.CrossPostTargets.PreviewCrossPost;
using TgPoster.API.Domain.UseCases.CrossPostTargets.UpdateCrossPostTarget;
using TgPoster.API.Models;

namespace TgPoster.API.Controllers;

/// <summary>
///     Контроллер связок расписаний с аккаунтами соцсетей
/// </summary>
/// <param name="sender"></param>
[Authorize]
[ApiController]
[Tags("CrossPostTarget")]
public class CrossPostTargetController(ISender sender) : ControllerBase
{
	/// <summary>
	///     Получение связок расписания
	/// </summary>
	/// <param name="scheduleId">Идентификатор расписания</param>
	/// <param name="ct">Токен отмены операции</param>
	/// <returns>Список связок расписания</returns>
	[HttpGet(Routes.CrossPostTarget.List)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<CrossPostTargetResponse>))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> List([FromRoute] Guid scheduleId, CancellationToken ct)
	{
		var result = await sender.Send(new ListCrossPostTargetsQuery(scheduleId), ct);
		return Ok(result);
	}

	/// <summary>
	///     Создание связки расписания с аккаунтом соцсети
	/// </summary>
	/// <param name="scheduleId">Идентификатор расписания</param>
	/// <param name="request">Данные связки</param>
	/// <param name="ct">Токен отмены операции</param>
	/// <returns>Идентификатор созданной связки</returns>
	[HttpPost(Routes.CrossPostTarget.Create)]
	[ProducesResponseType(StatusCodes.Status201Created, Type = typeof(CreateCrossPostTargetResponse))]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> Create(
		[FromRoute] Guid scheduleId,
		[FromBody] CreateCrossPostTargetRequest request,
		CancellationToken ct)
	{
		var command = new CreateCrossPostTargetCommand(
			scheduleId,
			request.SocialAccountId,
			request.Format,
			request.LinkTarget,
			request.CustomLink,
			request.CallToAction,
			request.IncludeMedia,
			request.IncludeParsed,
			request.DelayMinutes);

		var response = await sender.Send(command, ct);

		return Created(Routes.CrossPostTarget.Create, response);
	}

	/// <summary>
	///     Обновление связки расписания
	/// </summary>
	/// <param name="scheduleId">Идентификатор расписания</param>
	/// <param name="id">Идентификатор связки</param>
	/// <param name="request">Данные связки</param>
	/// <param name="ct">Токен отмены операции</param>
	/// <returns>Результат обновления</returns>
	[HttpPut(Routes.CrossPostTarget.Update)]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> Update(
		[FromRoute] Guid scheduleId,
		[FromRoute] Guid id,
		[FromBody] UpdateCrossPostTargetRequest request,
		CancellationToken ct)
	{
		var command = new UpdateCrossPostTargetCommand(
			scheduleId,
			id,
			request.IsActive,
			request.Format,
			request.LinkTarget,
			request.CustomLink,
			request.CallToAction,
			request.IncludeMedia,
			request.IncludeParsed,
			request.DelayMinutes);

		await sender.Send(command, ct);

		return NoContent();
	}

	/// <summary>
	///     Удаление связки расписания
	/// </summary>
	/// <param name="scheduleId">Идентификатор расписания</param>
	/// <param name="id">Идентификатор связки</param>
	/// <param name="ct">Токен отмены операции</param>
	/// <returns>Результат удаления</returns>
	[HttpDelete(Routes.CrossPostTarget.Delete)]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> Delete([FromRoute] Guid scheduleId, [FromRoute] Guid id, CancellationToken ct)
	{
		await sender.Send(new DeleteCrossPostTargetCommand(scheduleId, id), ct);
		return NoContent();
	}

	/// <summary>
	///     Предпросмотр кросс-поста для связок расписания
	/// </summary>
	/// <param name="scheduleId">Идентификатор расписания</param>
	/// <param name="request">Параметры предпросмотра</param>
	/// <param name="ct">Токен отмены операции</param>
	/// <returns>Список предпросмотров по аккаунтам</returns>
	[HttpPost(Routes.CrossPostTarget.Preview)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<CrossPostPreviewResponse>))]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> Preview(
		[FromRoute] Guid scheduleId,
		[FromBody] PreviewCrossPostRequest request,
		CancellationToken ct)
	{
		var query = new PreviewCrossPostQuery(
			scheduleId,
			request.SocialAccountId,
			request.Format,
			request.LinkTarget,
			request.CustomLink,
			request.CallToAction,
			request.Text);

		var result = await sender.Send(query, ct);

		return Ok(result);
	}
}
