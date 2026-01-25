using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TgPoster.API.Common;
using TgPoster.API.Domain.UseCases.Repost.AddRepostDestination;
using TgPoster.API.Domain.UseCases.Repost.CreateRepostSettings;
using TgPoster.API.Domain.UseCases.Repost.DeleteRepostDestination;
using TgPoster.API.Domain.UseCases.Repost.DeleteRepostSettings;
using TgPoster.API.Domain.UseCases.Repost.GetRepostSettings;
using TgPoster.API.Domain.UseCases.Repost.UpdateRepostDestination;
using TgPoster.API.Models;

namespace TgPoster.API.Controllers;

/// <summary>
///     Контроллер для управления репостами сообщений.
/// </summary>
[Authorize]
[ApiController]
public sealed class RepostController(ISender sender) : ControllerBase
{
	/// <summary>
	///     Создание настроек репоста для расписания.
	/// </summary>
	/// <param name="request">Данные для создания настроек репоста</param>
	/// <param name="ct">Токен отмены операции</param>
	/// <returns>Созданные настройки репоста</returns>
	[HttpPost(Routes.Repost.CreateSettings)]
	[ProducesResponseType(StatusCodes.Status201Created, Type = typeof(CreateRepostSettingsResponse))]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> CreateSettings(
		[FromBody] [Required] CreateRepostSettingsRequest request,
		CancellationToken ct)
	{
		var command = new CreateRepostSettingsCommand(
			request.ScheduleId,
			request.TelegramSessionId,
			request.Destinations);

		var result = await sender.Send(command, ct);

		return CreatedAtAction(
			nameof(GetSettings),
			new { scheduleId = request.ScheduleId },
			result);
	}

	/// <summary>
	///     Получение настроек репоста для расписания.
	/// </summary>
	/// <param name="scheduleId">ID расписания</param>
	/// <param name="ct">Токен отмены операции</param>
	/// <returns>Настройки репоста или 404 если не найдены</returns>
	[HttpGet(Routes.Repost.GetSettings)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(GetRepostSettingsResponse))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> GetSettings([FromRoute] [Required] Guid scheduleId, CancellationToken ct)
	{
		var query = new GetRepostSettingsQuery(scheduleId);
		var response = await sender.Send(query, ct);

		return Ok(response);
	}

	/// <summary>
	///     Удаление настроек репоста.
	/// </summary>
	/// <param name="id">ID настроек репоста</param>
	/// <param name="ct">Токен отмены операции</param>
	/// <returns>204 No Content при успешном удалении</returns>
	[HttpDelete(Routes.Repost.DeleteSettings)]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> DeleteSettings([FromRoute] [Required] Guid id, CancellationToken ct)
	{
		var command = new DeleteRepostSettingsCommand(id);
		await sender.Send(command, ct);

		return NoContent();
	}

	/// <summary>
	///     Добавление целевого канала для репоста.
	/// </summary>
	/// <param name="settingsId">ID настроек репоста</param>
	/// <param name="request">Данные целевого канала</param>
	/// <param name="ct">Токен отмены операции</param>
	/// <returns>Добавленный целевой канал</returns>
	[HttpPost(Routes.Repost.AddDestination)]
	[ProducesResponseType(StatusCodes.Status201Created, Type = typeof(AddRepostDestinationResponse))]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> AddDestination(
		[FromRoute] [Required] Guid settingsId,
		[FromBody] [Required] AddRepostDestinationRequest request,
		CancellationToken ct)
	{
		var command = new AddRepostDestinationCommand(settingsId, request.ChatIdentifier);
		var result = await sender.Send(command, ct);

		return CreatedAtAction(
			nameof(GetSettings),
			new { scheduleId = settingsId },
			result);
	}

	/// <summary>
	///     Удаление целевого канала.
	/// </summary>
	/// <param name="id">ID целевого канала</param>
	/// <param name="ct">Токен отмены операции</param>
	/// <returns>204 No Content при успешном удалении</returns>
	[HttpDelete(Routes.Repost.DeleteDestination)]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> DeleteDestination([FromRoute] [Required] Guid id, CancellationToken ct)
	{
		var command = new DeleteRepostDestinationCommand(id);
		await sender.Send(command, ct);

		return NoContent();
	}

	/// <summary>
	///     Обновление статуса целевого канала (активен/неактивен).
	/// </summary>
	/// <param name="id">ID целевого канала</param>
	/// <param name="request">Данные для обновления</param>
	/// <param name="ct">Токен отмены операции</param>
	/// <returns>204 No Content при успешном обновлении</returns>
	[HttpPut(Routes.Repost.UpdateDestination)]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> UpdateDestination(
		[FromRoute] [Required] Guid id,
		[FromBody] [Required] UpdateRepostDestinationRequest request,
		CancellationToken ct)
	{
		var command = new UpdateRepostDestinationCommand(id, request.IsActive);
		await sender.Send(command, ct);

		return NoContent();
	}
}
