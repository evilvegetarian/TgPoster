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
using TgPoster.API.Domain.UseCases.Repost.ListRepostSettings;
using TgPoster.API.Domain.UseCases.Repost.RefreshDestinationInfo;
using TgPoster.API.Domain.UseCases.Repost.UpdateRepostDestination;
using TgPoster.API.Domain.UseCases.Repost.UpdateRepostSettings;
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
			nameof(ListSettings),
			null,
			result);
	}

	/// <summary>
	///     Получение списка всех настроек репоста пользователя.
	/// </summary>
	/// <param name="ct">Токен отмены операции</param>
	/// <returns>Список настроек репоста</returns>
	[HttpGet(Routes.Repost.ListSettings)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ListRepostSettingsResponse))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> ListSettings(CancellationToken ct)
	{
		var response = await sender.Send(new ListRepostSettingsQuery(), ct);
		return Ok(response);
	}

	/// <summary>
	///     Получение подробной информации о настройках репоста.
	/// </summary>
	/// <param name="id">ID настроек репоста</param>
	/// <param name="ct">Токен отмены операции</param>
	/// <returns>Подробная информация о настройках репоста с каналами</returns>
	[HttpGet(Routes.Repost.GetSettings)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(RepostSettingsResponse))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> GetSettings([FromRoute] [Required] Guid id, CancellationToken ct)
	{
		var response = await sender.Send(new GetRepostSettingsQuery(id), ct);
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
	///     Обновление настроек репоста (рандомность, активность).
	/// </summary>
	/// <param name="id">ID настроек репоста</param>
	/// <param name="request">Данные для обновления</param>
	/// <param name="ct">Токен отмены операции</param>
	/// <returns>204 No Content при успешном обновлении</returns>
	[HttpPut(Routes.Repost.UpdateSettings)]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> UpdateSettings(
		[FromRoute] [Required] Guid id,
		[FromBody] [Required] UpdateRepostSettingsRequest request,
		CancellationToken ct)
	{
		var command = new UpdateRepostSettingsCommand(id, request.IsActive);

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
			nameof(ListSettings),
			null,
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
		var command = new UpdateRepostDestinationCommand(
			id,
			request.IsActive,
			request.DelayMinSeconds,
			request.DelayMaxSeconds,
			request.RepostEveryNth,
			request.SkipProbability,
			request.MaxRepostsPerDay);
		await sender.Send(command, ct);

		return NoContent();
	}

	/// <summary>
	///     Обновление информации о целевом канале из Telegram.
	/// </summary>
	/// <param name="id">ID целевого канала</param>
	/// <param name="ct">Токен отмены операции</param>
	/// <returns>204 No Content при успешном обновлении</returns>
	[HttpPost(Routes.Repost.RefreshDestination)]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> RefreshDestination([FromRoute] [Required] Guid id, CancellationToken ct)
	{
		await sender.Send(new RefreshDestinationInfoCommand(id), ct);
		return NoContent();
	}
}
