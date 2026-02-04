using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TgPoster.API.Common;
using TgPoster.API.Domain.UseCases.CommentRepost.CreateCommentRepost;
using TgPoster.API.Domain.UseCases.CommentRepost.DeleteCommentRepost;
using TgPoster.API.Domain.UseCases.CommentRepost.GetCommentRepost;
using TgPoster.API.Domain.UseCases.CommentRepost.ListCommentRepost;
using TgPoster.API.Domain.UseCases.CommentRepost.UpdateCommentRepost;
using TgPoster.API.Models;

namespace TgPoster.API.Controllers;

/// <summary>
///     Контроллер для управления комментирующими репостами.
/// </summary>
[Authorize]
[ApiController]
public sealed class CommentRepostController(ISender sender) : ControllerBase
{
	/// <summary>
	///     Создание настроек комментирующего репоста.
	/// </summary>
	/// <param name="request">Данные для создания</param>
	/// <param name="ct">Токен отмены операции</param>
	/// <returns>Созданные настройки</returns>
	[HttpPost(Routes.CommentRepost.Create)]
	[ProducesResponseType(StatusCodes.Status201Created, Type = typeof(CreateCommentRepostResponse))]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> Create(
		[FromBody] [Required] CreateCommentRepostRequest request,
		CancellationToken ct)
	{
		var command = new CreateCommentRepostCommand(
			request.ScheduleId,
			request.TelegramSessionId,
			request.WatchedChannel);

		var result = await sender.Send(command, ct);

		return CreatedAtAction(
			nameof(Get),
			new { id = result.Id },
			result);
	}

	/// <summary>
	///     Получение списка настроек комментирующего репоста.
	/// </summary>
	/// <param name="ct">Токен отмены операции</param>
	/// <returns>Список настроек</returns>
	[HttpGet(Routes.CommentRepost.List)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ListCommentRepostResponse))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> List(CancellationToken ct)
	{
		var response = await sender.Send(new ListCommentRepostQuery(), ct);
		return Ok(response);
	}

	/// <summary>
	///     Получение подробной информации о настройках комментирующего репоста.
	/// </summary>
	/// <param name="id">ID настроек</param>
	/// <param name="ct">Токен отмены операции</param>
	/// <returns>Подробная информация</returns>
	[HttpGet(Routes.CommentRepost.Get)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(GetCommentRepostResponse))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> Get([FromRoute] [Required] Guid id, CancellationToken ct)
	{
		var response = await sender.Send(new GetCommentRepostQuery(id), ct);
		return Ok(response);
	}

	/// <summary>
	///     Обновление настроек комментирующего репоста (активен/неактивен).
	/// </summary>
	/// <param name="id">ID настроек</param>
	/// <param name="request">Данные для обновления</param>
	/// <param name="ct">Токен отмены операции</param>
	/// <returns>204 No Content при успешном обновлении</returns>
	[HttpPut(Routes.CommentRepost.Update)]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> Update(
		[FromRoute] [Required] Guid id,
		[FromBody] [Required] UpdateCommentRepostRequest request,
		CancellationToken ct)
	{
		var command = new UpdateCommentRepostCommand(id, request.IsActive);
		await sender.Send(command, ct);

		return NoContent();
	}

	/// <summary>
	///     Удаление настроек комментирующего репоста.
	/// </summary>
	/// <param name="id">ID настроек</param>
	/// <param name="ct">Токен отмены операции</param>
	/// <returns>204 No Content при успешном удалении</returns>
	[HttpDelete(Routes.CommentRepost.Delete)]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> Delete([FromRoute] [Required] Guid id, CancellationToken ct)
	{
		await sender.Send(new DeleteCommentRepostCommand(id), ct);
		return NoContent();
	}
}
