using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TgPoster.API.Common;
using TgPoster.API.Domain.UseCases.Discover.GetDiscoverStatus;
using TgPoster.API.Domain.UseCases.Messages.ListMessage;
using TgPoster.API.Domain.UseCases.Parse.CreateParseChannel;
using TgPoster.API.Domain.UseCases.Parse.DeleteParseChannel;
using TgPoster.API.Domain.UseCases.Parse.GetParseChannelStats;
using TgPoster.API.Domain.UseCases.Parse.GetParseChannelStatus;
using TgPoster.API.Domain.UseCases.Parse.ListParseChannel;
using TgPoster.API.Domain.UseCases.Parse.ListParseChannelRuns;
using TgPoster.API.Domain.UseCases.Parse.RefreshParseChannelInfo;
using TgPoster.API.Mapper;
using TgPoster.API.Models;

namespace TgPoster.API.Controllers;

/// <summary>
///     Контроллер управления парсингом каналов
/// </summary>
/// <param name="sender"></param>
[Authorize]
[ApiController]
public class ParseChannelController(ISender sender) : ControllerBase
{
	/// <summary>
	///     Создания задания для парсинга канала
	/// </summary>
	/// <param name="request">Данные для создания задачи парсинга канала</param>
	/// <param name="ct">Токен отмены операции</param>
	/// <returns>Ответ с данными созданной задачи парсинга</returns>
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
	/// <param name="ct">Токен отмены операции</param>
	/// <returns>Список всех задач парсинга каналов</returns>
	[HttpGet(Routes.ParseChannel.List)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ParseChannelListResponse))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> List(CancellationToken ct)
	{
		var list = await sender.Send(new ListParseChannelsQuery(), ct);
		return Ok(list);
	}

	/// <summary>
	///     Изменения задачи на парсинг
	/// </summary>
	/// <param name="id">Идентификатор задачи парсинга для обновления</param>
	/// <param name="request">Данные для обновления задачи парсинга</param>
	/// <param name="ct">Токен отмены операции</param>
	/// <returns>Результат выполнения операции</returns>
	[HttpPut(Routes.ParseChannel.Update)]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> Update(
		[FromRoute] [Required] Guid id,
		[FromBody] [Required] UpdateParseChannelRequest request,
		CancellationToken ct
	)
	{
		await sender.Send(request.ToCommand(id), ct);
		return NoContent();
	}

	/// <summary>
	///     Удаления задачи на парсинг
	/// </summary>
	/// <param name="id">Идентификатор задачи парсинга для удаления</param>
	/// <param name="ct">Токен отмены операции</param>
	/// <returns>Результат выполнения операции</returns>
	[HttpDelete(Routes.ParseChannel.Delete)]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> Delete([FromRoute] [Required] Guid id, CancellationToken ct)
	{
		await sender.Send(new DeleteParseChannelCommand(id), ct);
		return NoContent();
	}

	/// <summary>
	///     Обновление информации о канале парсинга (количество сообщений)
	/// </summary>
	/// <param name="id">Идентификатор задачи парсинга</param>
	/// <param name="ct">Токен отмены операции</param>
	/// <returns>Результат выполнения операции</returns>
	[HttpPost(Routes.ParseChannel.Refresh)]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> Refresh([FromRoute] [Required] Guid id, CancellationToken ct)
	{
		await sender.Send(new RefreshParseChannelInfoCommand(id), ct);
		return NoContent();
	}

	/// <summary>
	/// Статистика парсинга по своим источникам: посты, отсев, журнал проходов и таймлайны по дням
	/// </summary>
	/// <param name="request">Глубина таймлайнов</param>
	/// <param name="ct">Токен отмены операции</param>
	/// <returns>Статистика парсинга</returns>
	[HttpGet(Routes.ParseChannel.Stats)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ParseChannelStatsResponse))]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> GetStats([FromQuery] GetDiscoverStatsRequest request, CancellationToken ct)
	{
		var stats = await sender.Send(new GetParseChannelStatsQuery(request.Days), ct);
		return Ok(stats);
	}

	/// <summary>
	/// Журнал парсинга по своим источникам, от свежих проходов к старым
	/// </summary>
	/// <param name="request">Фильтры и пагинация</param>
	/// <param name="ct">Токен отмены операции</param>
	/// <returns>Страница журнала</returns>
	[HttpGet(Routes.ParseChannel.Runs)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedResponse<ParseChannelRunResponse>))]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> GetRuns([FromQuery] ListParseChannelRunsRequest request, CancellationToken ct)
	{
		var response = await sender.Send(request.ToDomain(), ct);
		return Ok(response);
	}

	/// <summary>
	/// Состояние фоновой задачи парсинга каналов
	/// </summary>
	/// <param name="ct">Токен отмены операции</param>
	/// <returns>Статус, прогресс и расписание задачи</returns>
	[HttpGet(Routes.ParseChannel.Status)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DiscoverStatusResponse))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> GetStatus(CancellationToken ct)
	{
		var status = await sender.Send(new GetParseChannelStatusQuery(), ct);
		return Ok(status);
	}
}